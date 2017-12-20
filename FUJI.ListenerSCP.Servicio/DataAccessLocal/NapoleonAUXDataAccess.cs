using System;
using System.Linq;

namespace FUJI.ListenerSCP.Servicio.DataAccessLocal
{
    public class NapoleonAUXDataAccess
    {
        
        public static NAPOLEONAUXEntities NapAuxDA;

        public static void setEstudio(DataAccessLocal.tbl_MST_EstudioAUX mdlEstudio, DataAccessLocal.tbl_DET_EstudioAUX mdlDetalle)
        {
            try
            {
                using (NapAuxDA = new NAPOLEONAUXEntities())
                {
                    if (!NapAuxDA.tbl_MST_EstudioAUX.Any(x => x.id_Sitio == mdlEstudio.id_Sitio && x.vchAccessionNumber.Trim() == mdlEstudio.vchAccessionNumber))
                    {
                        NapAuxDA.tbl_MST_EstudioAUX.Add(mdlEstudio);
                        NapAuxDA.SaveChanges();
                        mdlDetalle.intEstudioID = mdlEstudio.intEstudioID;
                        if (mdlDetalle.intEstudioID > 0)
                        {
                            using (NapAuxDA = new NAPOLEONAUXEntities())
                            {
                                NapAuxDA.tbl_DET_EstudioAUX.Add(mdlDetalle);
                                NapAuxDA.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        mdlDetalle.intEstudioID = NapAuxDA.tbl_MST_EstudioAUX.First(x => x.id_Sitio == mdlEstudio.id_Sitio && x.vchAccessionNumber.Trim() == mdlEstudio.vchAccessionNumber).intEstudioID;
                        if (mdlDetalle.intEstudioID > 0)
                        {
                            using (NapAuxDA = new NAPOLEONAUXEntities())
                            {
                                NapAuxDA.tbl_DET_EstudioAUX.Add(mdlDetalle);
                                NapAuxDA.SaveChanges();
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

        public static int getConsecutivo()
        {
            int vchAccessionNumber = 0;
            try
            {
                using (NapAuxDA = new NAPOLEONAUXEntities())
                {
                    if (NapAuxDA.tbl_MST_EstudioAUX.Any())
                    {
                        vchAccessionNumber = NapAuxDA.tbl_MST_EstudioAUX.Max(x => x.intEstudioID) + 1;
                    }
                    else
                    {
                        vchAccessionNumber = 1;
                    }
                }
            }
            catch(Exception egAN)
            {
                Log.EscribeLog("Existe un error en getConsecutivo: " + egAN.Message);
            }
            return vchAccessionNumber;
        }

        public static string getAccNumber(string PatientID, int id_Sitio)
        {
            Log.EscribeLog("Id_Sitio: " + id_Sitio + " , Paciente_: " + PatientID);
            string vchAccessionNumber = "";
            try
            {
                using (NapAuxDA = new NAPOLEONAUXEntities())
                {
                    if (NapAuxDA.tbl_MST_EstudioAUX.Any(x => x.PatientID == PatientID && x.id_Sitio == id_Sitio))
                    {
                        vchAccessionNumber = NapAuxDA.tbl_MST_EstudioAUX.First(x => x.PatientID.ToUpper() == PatientID.ToUpper() && x.id_Sitio == id_Sitio).vchAccessionNumber;
                    }
                }
            }
            catch (Exception egAN)
            {
                Log.EscribeLog("Existe un error en getAccNumber: " + egAN.Message);
            }
            return vchAccessionNumber;
        }

        public static string getPatientID()
        {
            string vchName = "";
            try
            {
                using (NapAuxDA = new NAPOLEONAUXEntities())
                {
                    if (NapAuxDA.tbl_MST_EstudioAUX.Any())
                    {
                        vchName = NapAuxDA.tbl_MST_EstudioAUX.Max(x => x.intEstudioID).ToString();
                    }
                    else
                    {
                        vchName = "1";
                    }
                }
            }
            catch (Exception egAN)
            {
                Log.EscribeLog("Existe un error en getPatientID: " + egAN.Message);
            }
            return vchName;
        }
    }
}
