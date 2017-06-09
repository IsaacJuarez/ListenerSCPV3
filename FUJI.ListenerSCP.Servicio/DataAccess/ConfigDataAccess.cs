using System;
using System.Linq;

namespace FUJI.ListenerSCP.Servicio.DataAccess
{
    public class ConfigDataAccess
    {
        public static dbConfigEntities ConfigDA;
        public static void setService(int id_Servicio, string vchClaveSitio)
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
                            DataAccess.tbl_ConfigSitio mdlSitio = new DataAccess.tbl_ConfigSitio();
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

        public static DataAccess.tbl_ConfigSitio getConeccion(string vchClaveSitio)
        {
            DataAccess.tbl_ConfigSitio mdl = new DataAccess.tbl_ConfigSitio();
            try
            {
                using (ConfigDA = new dbConfigEntities())
                {
                    if (ConfigDA.tbl_ConfigSitio.Any(item => (bool)item.bitActivo))
                    {
                        var query = (from item in ConfigDA.tbl_ConfigSitio
                                     where (bool)item.bitActivo && item.vchClaveSitio == vchClaveSitio
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
    }
}
