using Dicom;
using Dicom.Log;
using Dicom.Network;
using FUJI.ListenerSCP.Servicio.DataAccess;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace FUJI.ListenerSCP.Servicio
{
    public partial class ListenerSCPService : ServiceBase
    {
        
        public static clsConfiguracion _conf;
        public ListenerSCPService()
        {
            Log.EscribeLog("Inicio del Servicio SCP");
            cargarServicio();
        }

        public dbConfigEntities ConfigDA;

        public tbl_ConfigSitio getConeccion()
        {
            tbl_ConfigSitio mdl = new tbl_ConfigSitio();
            try
            {
                using (ConfigDA = new dbConfigEntities())
                {
                    if (ConfigDA.tbl_ConfigSitio.Any(item => (bool)item.bitActivo))
                    {
                        var query = (from item in ConfigDA.tbl_ConfigSitio
                                     where (bool)item.bitActivo
                                     select item);
                        if (query != null)
                        {
                            if (query.Count() > 0)
                            {
                                mdl = query.First();
                            }
                        }
                    }
                }
                Log.EscribeLog("Se consulta la configuración: Puerto Cliente: " + mdl.intPuertoCliente);
            }
            catch (Exception egc)
            {
                Log.EscribeLog("Existe un error al obtner las configuraciones para el Servicio SCP: " + egc);
            }
            return mdl;
        }

        private void cargarServicio()
        {
            try
            {
                string path = "";
                try
                {
                    path = ConfigurationManager.AppSettings["ConfigDirectory"] != null ? ConfigurationManager.AppSettings["ConfigDirectory"].ToString() : "";
                }
                catch(Exception ePath)
                {
                    path = "";
                    Log.EscribeLog("Error al obtener el path desde appSettings: " + ePath.Message);
                }
                tbl_ConfigSitio mdl = new tbl_ConfigSitio();
                if (File.Exists(path + "info.xml"))
                {
                    _conf = XMLConfigurator.getXMLfile();
                }

                mdl = getConeccion();

                if (mdl != null || _conf != null)
                {
                    Log.EscribeLog("Inicio de CargarServicio SCP");
                    // preload dictionary to prevent timeouts
                    var dict = DicomDictionary.Default;
                    int port = 0;

                    Log.EscribeLog("Puerto: " + mdl.intPuertoCliente);
                    // start DICOM server on port from command line argument or 11112
                    try
                    {
                        if (_conf.intPuertoCliente > 0)
                        {
                            port = _conf.intPuertoCliente;
                        }
                        else
                        {
                            if (mdl.intPuertoCliente > 0)
                            {
                                port = (int)mdl.intPuertoCliente;
                            }
                            else
                            {
                                port = Convert.ToInt32(ConfigurationManager.AppSettings["Puerto"].ToString());
                            }
                        }
                    }
                    catch (Exception ePuerto)
                    {
                        Console.WriteLine("No se pudo leer el puerto especificado, favor de verificar.: " + ePuerto.Message);
                        Log.EscribeLog("No se pudo leer el puerto especificado, favor de verificar.: " + ePuerto.Message);
                    }
                    if (port > 0)
                    {
                        Console.WriteLine($"Iniciando Servidor C-Store SCP en el  puerto {port}");

                        var server = DicomServer.Create<CStoreSCP>(port);
                        Log.EscribeLog($"Iniciando Servidor C-Store SCP en el  puerto {port}");

                        // end process
                        Console.WriteLine("Oprimir <return> para finalizar...");
                        Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("No se pudo leer el puerto especificado, favor de verificar.");
                        Log.EscribeLog("No se pudo leer el puerto especificado, favor de verificar.");
                    }
                }
            }
            catch (Exception eLoadService)
            {
                Log.EscribeLog("Error al cargar el servicio: " + eLoadService.Message);
            }
        }

        private class CStoreSCP : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
        {
            private static DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
                                                                                {
                                                                                    DicomTransferSyntax
                                                                                        .ExplicitVRLittleEndian,
                                                                                    DicomTransferSyntax
                                                                                        .ExplicitVRBigEndian,
                                                                                    DicomTransferSyntax
                                                                                        .ImplicitVRLittleEndian
                                                                                };

            private static DicomTransferSyntax[] AcceptedImageTransferSyntaxes = new DicomTransferSyntax[]
                                                                                     {
                                                                                         // Lossless
                                                                                         DicomTransferSyntax
                                                                                             .JPEGLSLossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEG2000Lossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess14SV1,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess14,
                                                                                         DicomTransferSyntax
                                                                                             .RLELossless,

                                                                                         // Lossy
                                                                                         DicomTransferSyntax
                                                                                             .JPEGLSNearLossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEG2000Lossy,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess1,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess2_4,

                                                                                         // Uncompressed
                                                                                         DicomTransferSyntax
                                                                                             .ExplicitVRLittleEndian,
                                                                                         DicomTransferSyntax
                                                                                             .ExplicitVRBigEndian,
                                                                                         DicomTransferSyntax
                                                                                             .ImplicitVRLittleEndian
                                                                                     };

            public CStoreSCP(INetworkStream stream, Encoding fallbackEncoding, Logger log)
                : base(stream, fallbackEncoding, log)
            {
            }

            public void OnReceiveAssociationRequest(DicomAssociation association)
            {
                if (association.CalledAE != "STORESCP")
                {
                    SendAssociationReject(
                        DicomRejectResult.Permanent,
                        DicomRejectSource.ServiceUser,
                        DicomRejectReason.CalledAENotRecognized);
                    return;
                }

                foreach (var pc in association.PresentationContexts)
                {
                    if (pc.AbstractSyntax == DicomUID.Verification) pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                    else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None) pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
                }

                SendAssociationAccept(association);
            }

            public void OnReceiveAssociationReleaseRequest()
            {
                SendAssociationReleaseResponse();
            }

            public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
            {
            }

            public void OnConnectionClosed(Exception exception)
            {
            }

            public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request)
            {
                try
                {
                    //public static string PathDestino = ConfigurationManager.AppSettings["PathDes"].ToString();
                    string PathDestino = "";
                    try
                    {
                        PathDestino = ConfigurationManager.AppSettings["PathDes"].ToString();
                    }
                    catch (Exception ePATHDES)
                    {
                        Log.EscribeLog("Existe un error al leer el path de destino: " + ePATHDES.Message);
                    }
                    if (PathDestino != "")
                    {
                        var studyUid = request.Dataset.Get<string>(DicomTag.StudyInstanceUID);
                        var instUid = request.SOPInstanceUID.UID;
                        Console.WriteLine(instUid.ToString());
                        Log.EscribeLog("Leyendo: " + instUid.ToString());
                        var path = Path.GetFullPath(PathDestino);
                        path = Path.Combine(path, studyUid);

                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        path = Path.Combine(path, instUid) + ".dcm";

                        request.File.Save(path);
                    }
                    else
                    {
                        Log.EscribeLog("No se encontro el path.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Existe un error: " + e.Message);
                    Log.EscribeLog("Existe un error: " + e.Message);
                }
                return new DicomCStoreResponse(request, DicomStatus.Success);
            }

            public void OnCStoreRequestException(string tempFileName, Exception e)
            {
                // let library handle logging and error response
            }

            public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
            {
                return new DicomCEchoResponse(request, DicomStatus.Success);
            }
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }
    }
}
