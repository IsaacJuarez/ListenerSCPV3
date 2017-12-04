using Dicom;
using Dicom.Log;
using Dicom.Network;
using FUJI.ListenerSCP.Servicio.DataAccess;
using FUJI.ListenerSCP.Servicio.DataAccessLocal;
using FUJI.ListenerSCP.Servicio.Feed2Service;
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
        public static string Token = "";

        public static clsConfiguracion _conf;
        public ListenerSCPService()
        {
            Log.EscribeLog("Inicio del Servicio SCP");
            cargarServicio();
        }

        public static NAPOLEONAUXEntities NapAuxDA;


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
                string PathDestino = "";
                try
                {
                    PathDestino = ConfigurationManager.AppSettings["PathDes"].ToString();
                    //Verificar Folder
                    if (!Directory.Exists(PathDestino))
                        Directory.CreateDirectory(PathDestino);
                }
                catch (Exception ePATHDES)
                {
                    Log.EscribeLog("Existe un error al leer el path de destino: " + ePATHDES.Message);
                }

                ClienteF2CResponse mdl = new ClienteF2CResponse();
                if (File.Exists(path + "info.xml"))
                {
                    _conf = XMLConfigurator.getXMLfile();
                    id_Servicio = _conf.id_Sitio;
                    AETitle = _conf.vchAETitle;
                    vchPathRep = _conf.vchPathLocal;
                    vchClaveSitio = _conf.vchClaveSitio;

                    if (id_Servicio > 0 && vchClaveSitio != "")
                        Token = Security.Encrypt(id_Servicio + "|" + vchClaveSitio);
                }
                Log.EscribeLog("Sitio: " + vchClaveSitio);
                if (vchClaveSitio != "")
                {
                    mdl = ConfigDataAccess.getConeccion(vchClaveSitio, id_Servicio);
                    if (mdl != null || _conf != null)
                    {
                        if (!(id_Servicio > 0))
                        {
                            id_Servicio = mdl.id_Sitio;
                        }

                        if (vchClaveSitio == "")
                        {
                            vchClaveSitio = mdl.ConfigSitio.vchClaveSitio;
                        }
                        if (AETitle == "")
                        {
                            AETitle = mdl.ConfigSitio.vchAETitle;
                        }
                        if(Token != "")
                            Token = Security.Encrypt(id_Servicio + "|" + vchClaveSitio);
                        Log.EscribeLog("Inicio de CargarServicio SCP");
                        // preload dictionary to prevent timeouts
                        var dict = DicomDictionary.Default;
                        int port = 0;

                        Log.EscribeLog("Puerto: " + (mdl.ConfigSitio == null ? _conf.intPuertoCliente : mdl.ConfigSitio.intPuertoCliente) );
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
                                if (mdl.ConfigSitio.intPuertoCliente > 0)
                                {
                                    port = (int)mdl.ConfigSitio.intPuertoCliente;
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

                            ConfigDataAccess.setService(id_Servicio, vchClaveSitio);
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
                else
                {
                    Console.WriteLine("No se pudo encontrar los datos para la búsqueda de sitio. En espera de los datos de configuración.");
                    Log.EscribeLog("No se pudo encontrar los datos para la búsqueda de sitio. En espera de los datos de configuración.");
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
                        //Verificar Folder
                        if (!Directory.Exists(PathDestino))
                            Directory.CreateDirectory(PathDestino);
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
                        string studyUid = "";
                        string AccNum = "";
                        string Modality = "";
                        string Edad = "";
                        string PatientID = "";
                        string patienName = "";
                        string genero = "";
                        string instUid = "";
                        string FechaNac = "";
                        string UniversalServiceID = "";
                        string studyDescription = "";
                        try { studyUid = request.Dataset.Contains(DicomTag.StudyInstanceUID) ? request.Dataset.Get<string>(DicomTag.StudyInstanceUID) : ""; } catch (Exception eUI) { studyUid = ""; }
                        try { AccNum = request.Dataset.Contains(DicomTag.AccessionNumber) ? request.Dataset.Get<string>(DicomTag.AccessionNumber) : ""; } catch (Exception eUI) { AccNum = ""; }
                        try { Modality = request.Dataset.Contains(DicomTag.Modality) ? request.Dataset.Get<string>(DicomTag.Modality) : ""; } catch (Exception eUI) { Modality = ""; }
                        try { Edad = request.Dataset.Contains(DicomTag.PatientAge) ? request.Dataset.Get<string>(DicomTag.PatientAge) : ""; } catch (Exception eUI) { Edad = ""; }
                        try { FechaNac = request.Dataset.Contains(DicomTag.PatientBirthDate) ? request.Dataset.Get<string>(DicomTag.PatientBirthDate) : ""; } catch (Exception eUI) { FechaNac = ""; }
                        try { PatientID = request.Dataset.Contains(DicomTag.PatientID) ? request.Dataset.Get<string>(DicomTag.PatientID) : ""; } catch (Exception eUI) { PatientID = ""; }
                        try { patienName = request.Dataset.Contains(DicomTag.PatientName) ? request.Dataset.Get<string>(DicomTag.PatientName) : ""; } catch (Exception eUI) { patienName = ""; }
                        try { genero = request.Dataset.Contains(DicomTag.PatientSex) ? request.Dataset.Get<string>(DicomTag.PatientSex) : ""; } catch (Exception eUI) { genero = ""; }
                        try { instUid = request.SOPInstanceUID.UID; } catch (Exception eUI) { instUid = ""; }
                        try { UniversalServiceID = request.Dataset.Contains(DicomTag.StudyID) ? request.Dataset.Get<string>(DicomTag.StudyID) : ""; } catch (Exception eUI) { UniversalServiceID = ""; }
                        try { studyDescription = request.Dataset.Contains(DicomTag.StudyDescription) ? request.Dataset.Get<string>(DicomTag.StudyDescription) : ""; } catch (Exception eUI) { studyDescription = ""; }

                        Console.WriteLine(instUid.ToString());
                        Log.EscribeLog("Leyendo: " + instUid.ToString());
                        var path = Path.GetFullPath(vchPathRep);
                        path = Path.Combine(path, studyUid);

                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        path = Path.Combine(path, instUid) + ".dcm";
                        DataAccessLocal.tbl_MST_EstudioAUX mdlEstudio = new DataAccessLocal.tbl_MST_EstudioAUX();
                        DataAccessLocal.tbl_DET_EstudioAUX mdlDetalle = new DataAccessLocal.tbl_DET_EstudioAUX();
                        bool valido = false;
                        try
                        {
                            //Obtener MST
                            mdlEstudio.id_Sitio = id_Servicio;
                            mdlEstudio.intModalidadID = getModalidad(Modality);
                            mdlEstudio.PatientID = PatientID.Trim() == "" ? getPatientID(mdlEstudio.id_Sitio): PatientID;
                            mdlEstudio.vchPatientBirthDate = FechaNac;
                            mdlEstudio.PatientName = patienName.Replace("^^", " ").Replace("^^^", " ").Replace("^", " ");
                            mdlEstudio.datFecha = DateTime.Now;
                            mdlEstudio.vchgenero = genero;
                            mdlEstudio.vchEdad = Edad == "" && FechaNac != "" ? getEdad(FechaNac) : Edad;
                            mdlEstudio.vchAccessionNumber = AccNum.Trim() == "" ? getAccNumber((int)mdlEstudio.id_Sitio, (int)mdlEstudio.intModalidadID, mdlEstudio.PatientID) : AccNum;
                            mdlEstudio.StudyID = UniversalServiceID;
                            mdlEstudio.StudyDescription = studyDescription;

                            //Obtener DET

                            mdlDetalle.vchNameFile = Path.GetFileName(path);
                            mdlDetalle.vchPathFile = path.ToString();
                            mdlDetalle.vchStudyInstanceUID = studyUid;
                            mdlDetalle.datFecha = DateTime.Now;
                            mdlDetalle.intEstatusID = 1;
                            mdlDetalle.bitSync = false;
                            valido = true;
                        }
                        catch (Exception evalidar)
                        {
                            valido = false;
                            Log.EscribeLog("Existe un error al obtener el estudio: " + evalidar.Message);
                        }

                        try
                        {
                            request.Dataset.Remove(DicomTag.PatientID);
                            request.Dataset.Remove(DicomTag.AccessionNumber);
                            request.Dataset.Remove(DicomTag.PatientAge);
                            request.Dataset.Add(new DicomCodeString(DicomTag.AccessionNumber, mdlEstudio.vchAccessionNumber));
                            request.Dataset.Add(new DicomCodeString(DicomTag.PatientID, mdlEstudio.PatientID));
                            request.Dataset.Add(new DicomCodeString(DicomTag.PatientAge, mdlEstudio.PatientID));
                            request.File.Save(path);
                            long length = new System.IO.FileInfo(path).Length;
                            mdlDetalle.intSizeFile = (int)length;
                            ConfigDataAccess.setService(id_Servicio, vchClaveSitio);
                            if (valido)
                            {
                                NapoleonAUXDataAccess.setEstudio(mdlEstudio, mdlDetalle);
                            }
                        }
                        catch (Exception eSEND)
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

            private string getEdad(string _Edad)
            {
                string edad = "";
                try
                {
                    //DateTime fechaNacimiento = Convert.ToDateTime(_Edad);
                    string[] format = { "yyyyMMdd" };
                    DateTime date;
                    int anios = 0;
                    if (DateTime.TryParseExact(_Edad,
                           format,
                           System.Globalization.CultureInfo.InvariantCulture,
                           System.Globalization.DateTimeStyles.None,
                           out date))
                    {
                        anios = (DateTime.Today.Year - date.Year);
                        if (date > DateTime.Today.AddYears(-anios))
                            anios--;
                    }
                    
                    edad = anios.ToString();
                }
                catch (Exception egE)
                {
                    edad = "0";
                    Log.EscribeLog("Existe un error al calcular la edad: " + egE.Message);
                }
                return edad;
            }

            private string getPatientID(int? id_Sitio)
            {
                string PatientID = "";
                try
                {
                    string id = NapoleonAUXDataAccess.getPatientID();
                    PatientID = vchClaveSitio + (id == "" ? Guid.NewGuid().ToString() : id);
                }
                catch(Exception egPI)
                {
                    Log.EscribeLog("Existe error en getPatientID: " + egPI.Message);
                    PatientID = "";
                }
                return PatientID;
            }

            private string getAccNumber(int id_Sitio, int intModalidadID, string patientID)
            {
                string AccNum = "";
                string accAux = "";
                try
                {
                    int consec = 0;
                    try
                    {
                        accAux = NapoleonAUXDataAccess.getAccNumber(patientID.Trim(), id_Sitio);
                        if (accAux == "")
                            consec = NapoleonAUXDataAccess.getConsecutivo();
                    }
                    catch (Exception)
                    {
                        consec = 0;
                    }
                    if (accAux == "")
                    {
                        AccNum = id_Sitio.ToString() + intModalidadID.ToString() + (patientID.ToString().Trim() != "" ? patientID.ToString() : "XXXXX") + consec;
                    }
                    else
                    {
                        AccNum = accAux;
                    }
                }
                catch (Exception egAN)
                {
                    AccNum = "";
                    Log.EscribeLog("Existe error en getAccNumber: " + egAN.Message);
                }
                return AccNum;
            }

            private int? getModalidad(string modality)
            {
                int? intModalidadID = 0;
                try
                {
                    using(NapAuxDA = new NAPOLEONAUXEntities())
                    {
                        if (NapAuxDA.tbl_CAT_ModalidadAUX.Any(x => x.vchModalidadClave.Trim().ToUpper() == modality.ToUpper().Trim()))
                        {
                            intModalidadID = NapAuxDA.tbl_CAT_ModalidadAUX.First(x => x.vchModalidadClave.Trim().ToUpper() == modality.ToUpper().Trim()).intModalidadID;
                        }
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

                      
        }

        protected override void OnStart(string[] args)
        {

        }

        protected override void OnStop()
        {

        }
    }
}
