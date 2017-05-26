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
        public static string AETitle = "";
        public static string vchPathRep = "";
        public static int id_Servicio = 0;
        public static string vchClaveSitio = "";

        public static clsConfiguracion _conf;
        public ListenerSCPService()
        {
            Log.EscribeLog("Inicio del Servicio SCP");
            cargarServicio();
        }

        public static dbConfigEntities ConfigDA;

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

        public void setService()
        {
            try
            {
                tbl_DET_ServicioSitio mdl = new tbl_DET_ServicioSitio();

                if (id_Servicio > 0)
                {
                    using (ConfigDA = new dbConfigEntities())
                    {
                        if (ConfigDA.tbl_DET_ServicioSitio.Any(x => x.id_Sitio == id_Servicio))
                        {
                            using (ConfigDA = new dbConfigEntities())
                            {
                                mdl = ConfigDA.tbl_DET_ServicioSitio.First(x => x.id_Sitio == id_Servicio);
                                mdl.datFechaSCP = DateTime.Now;
                                ConfigDA.SaveChanges();
                            }
                        }
                        else
                        {
                            using (ConfigDA = new dbConfigEntities())
                            {
                                mdl.id_Sitio = id_Servicio;
                                mdl.datFechaSCP = DateTime.Now;
                                ConfigDA.tbl_DET_ServicioSitio.Add(mdl);
                                ConfigDA.SaveChanges();
                            }
                        }
                    }
                }
                else
                {
                    if (vchClaveSitio != "")
                    {
                        using (ConfigDA = new dbConfigEntities())
                        {
                            tbl_ConfigSitio mdlSitio = new tbl_ConfigSitio();
                            if (ConfigDA.tbl_ConfigSitio.Any(x => x.vchClaveSitio == vchClaveSitio))
                            {
                                mdlSitio = ConfigDA.tbl_ConfigSitio.First(x => x.vchClaveSitio == vchClaveSitio);
                                mdl = ConfigDA.tbl_DET_ServicioSitio.First(x => x.id_Sitio == mdlSitio.id_Sitio);
                                mdl.datFechaSCP = DateTime.Now;
                                ConfigDA.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception eSS)
            {
                Log.EscribeLog("Existe un error en setService: " + eSS.Message);
                //throw eSS;
            }
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
                    id_Servicio = _conf.id_Sitio;
                    AETitle = _conf.vchAETitle;
                    vchPathRep = _conf.vchPathLocal;
                    vchClaveSitio = _conf.vchClaveSitio;
                }

                mdl = getConeccion();

                if (mdl != null || _conf != null)
                {
                    if(!(id_Servicio >0))
                    {
                        id_Servicio = mdl.id_Sitio;
                    }

                    if(vchClaveSitio =="")
                    {
                        vchClaveSitio = mdl.vchClaveSitio;
                    }
                    if (AETitle == "")
                    {
                        AETitle = mdl.vchAETitle;
                    }
                    Log.EscribeLog("Inicio de CargarServicio SCP");
                    // preload dictionary to prevent timeouts
                    var dict = DicomDictionary.Default;
                    int port = 0;

                    Log.EscribeLog("Puerto: " + mdl.intPuertoCliente);
                    Log.EscribeLog("AETitle: " + AETitle);
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
                        setService();
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
                if (association.CalledAE != (AETitle == "" ? "STORESCP" : AETitle))
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
                    if (vchPathRep == "")
                    {
                        vchPathRep = PathDestino;
                    }
                    if (vchPathRep != "")
                    {
                        var studyUid = request.Dataset.Get<string>(DicomTag.StudyInstanceUID);
                        var AccNum = request.Dataset.Get<string>(DicomTag.AccessionNumber);
                        var Modality = request.Dataset.Get<string>(DicomTag.Modality);
                        var Edad = request.Dataset.Get<string>(DicomTag.PatientAge);
                        var FechaNac = request.Dataset.Get<string>(DicomTag.PatientBirthDate);
                        var PatientID = request.Dataset.Get<string>(DicomTag.PatientID);
                        var patienName = request.Dataset.Get<string>(DicomTag.PatientName);
                        var instUid = request.SOPInstanceUID.UID;

                        Console.WriteLine(instUid.ToString());
                        Log.EscribeLog("Leyendo: " + instUid.ToString());
                        var path = Path.GetFullPath(vchPathRep);
                        path = Path.Combine(path, studyUid);

                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        path = Path.Combine(path, instUid) + ".dcm";
                        tbl_MST_Estudio mdlEstudio = new tbl_MST_Estudio();
                        tbl_DET_Estudio mdlDetalle = new tbl_DET_Estudio();
                        bool valido = false;
                        try
                        {
                            //Obtener MST
                            mdlEstudio.vchAccessionNumber = AccNum;
                            mdlEstudio.id_Sitio = id_Servicio;
                            mdlEstudio.intModalidadID = getModalidad(Modality);
                            mdlEstudio.PatientID = PatientID;
                            mdlEstudio.vchPatientBirthDate = FechaNac;
                            mdlEstudio.PatientName = patienName;
                            mdlEstudio.datFecha = DateTime.Now;

                            //Obtener DET
                            long length = new System.IO.FileInfo(path).Length;
                            mdlDetalle.intSizeFile = (int)length;
                            mdlDetalle.vchNameFile = Path.GetFileName(path);
                            mdlDetalle.vchPathFile = path.ToString();
                            mdlDetalle.vchStudyInstanceUID = studyUid;
                            mdlDetalle.datFecha = DateTime.Now;
                            mdlDetalle.intEstatusID = 1;
                            valido = true;
                        }
                        catch(Exception evalidar)
                        {
                            valido = false;
                            Log.EscribeLog("Existe un error al obtener el estudio: " + evalidar.Message);
                        }

                        try
                        {
                            request.File.Save(path);
                            setService();
                            if (valido)
                            {
                                setEstudio(mdlEstudio, mdlDetalle);
                            }
                        }
                        catch(Exception eSEND)
                        {
                            Log.EscribeLog("Existe error al guardar el archivo: " + eSEND.Message);
                        }
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

            private int? getModalidad(string modality)
            {
                int? intModalidadID = 0;
                try
                {
                    using(ConfigDA = new dbConfigEntities())
                    {
                        intModalidadID = ConfigDA.tbl_CAT_Modalidad.First(x => x.vchModalidadClave.Trim().ToUpper() == modality.ToUpper().Trim()).intModalidadID;
                    }
                }
                catch(Exception egS)
                {
                    intModalidadID = 0;
                    Log.EscribeLog("Error en getModalidad: " + egS.Message);
                }
                return intModalidadID;
            }

            public void OnCStoreRequestException(string tempFileName, Exception e)
            {
                // let library handle logging and error response
            }

            public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
            {
                return new DicomCEchoResponse(request, DicomStatus.Success);
            }

            public void setService()
            {
                try
                {
                    tbl_DET_ServicioSitio mdl = new tbl_DET_ServicioSitio();

                    if (id_Servicio > 0)
                    {
                        using (ConfigDA = new dbConfigEntities())
                        {
                            mdl = ConfigDA.tbl_DET_ServicioSitio.First(x => x.id_Sitio == id_Servicio);
                            mdl.datFechaSCP = DateTime.Now;
                            ConfigDA.SaveChanges();
                        }
                    }
                    else
                    {
                        using (ConfigDA = new dbConfigEntities())
                        {
                            tbl_ConfigSitio mdlSitio = new tbl_ConfigSitio();
                            if (ConfigDA.tbl_ConfigSitio.Any(x => x.vchClaveSitio == vchClaveSitio))
                            {
                                mdlSitio = ConfigDA.tbl_ConfigSitio.First(x => x.vchClaveSitio == vchClaveSitio);
                                mdl = ConfigDA.tbl_DET_ServicioSitio.First(x => x.id_Sitio == mdlSitio.id_Sitio);
                                mdl.datFechaSCP = DateTime.Now;
                                ConfigDA.SaveChanges();
                            }
                        }
                    }
                }
                catch (Exception eSS)
                {
                    Log.EscribeLog("Existe un error en setService 2: " + eSS.Message);
                    //throw eSS;
                }
            }

            public void setEstudio(tbl_MST_Estudio mdlEstudio, tbl_DET_Estudio mdlDetalle)
            {
                try
                {
                    using (ConfigDA = new dbConfigEntities())
                    {
                        if (!ConfigDA.tbl_MST_Estudio.Any(x => x.id_Sitio == mdlEstudio.id_Sitio && x.vchAccessionNumber.Trim() == mdlEstudio.vchAccessionNumber))
                        {
                            ConfigDA.tbl_MST_Estudio.Add(mdlEstudio);
                            ConfigDA.SaveChanges();
                            mdlDetalle.intEstudioID = mdlEstudio.intEstudioID;
                            if (mdlDetalle.intEstudioID > 0)
                            {
                                using (ConfigDA = new dbConfigEntities())
                                {
                                    ConfigDA.tbl_DET_Estudio.Add(mdlDetalle);
                                    ConfigDA.SaveChanges();
                                }
                            }
                        }
                        else
                        {
                            mdlDetalle.intEstudioID = ConfigDA.tbl_MST_Estudio.First(x => x.id_Sitio == mdlEstudio.id_Sitio && x.vchAccessionNumber.Trim() == mdlEstudio.vchAccessionNumber).intEstudioID;
                            if (mdlDetalle.intEstudioID > 0)
                            {
                                using (ConfigDA = new dbConfigEntities())
                                {
                                    ConfigDA.tbl_DET_Estudio.Add(mdlDetalle);
                                    ConfigDA.SaveChanges();
                                }
                            }
                        }
                    }
                }
                catch (Exception esE)
                {
                    Log.EscribeLog("Existe un error en setEstudio: " + esE.Message);
                }
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
