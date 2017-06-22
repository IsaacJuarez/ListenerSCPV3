using FUJI.ListenerSCP.Servicio.Feed2Service;
using System;
using System.Linq;

namespace FUJI.ListenerSCP.Servicio.DataAccess
{
    public class ConfigDataAccess
    {
        public static NapoleonServiceClient NapoleonDA = new NapoleonServiceClient();


        public static void setService(int id_Servicio, string vchClaveSitio)
        {
            try
            {
                ClienteF2CRequest request = new ClienteF2CRequest();
                //request.Token = 
                request.id_Sitio = id_Servicio;
                //request.id_SitioSpecified = true;
                request.vchClaveSitio = vchClaveSitio;
                request.Token = Security.Encrypt(id_Servicio + "|" + vchClaveSitio);
                request.tipoServicio = 1;
                //request.tipoServicioSpecified = true;
                using (NapoleonDA = new NapoleonServiceClient())
                {
                    NapoleonDA.setService(request);
                }
            }
            catch (Exception eSS)
            {
                Log.EscribeLog("Existe un error en setService: " + eSS.Message);
                //throw eSS;
            }
        }

        public static ClienteF2CResponse getConeccion(string vchClaveSitio, int id_Sitio)
        {
            ClienteF2CResponse response = new ClienteF2CResponse();
            try
            {
                ClienteF2CRequest request = new ClienteF2CRequest();
                request.id_Sitio = id_Sitio;
                //request.id_SitioSpecified = true;
                request.vchClaveSitio = vchClaveSitio;
                request.Token = Security.Encrypt(id_Sitio + "|" + vchClaveSitio);
                //response = NapoleonDA.getConeccion(request);
                using(NapoleonDA = new NapoleonServiceClient())
                {
                    response = NapoleonDA.getConeccion(request);
                }
                Log.EscribeLog("Se consulta la configuración: Puerto Cliente: " + response.ConfigSitio.intPuertoCliente);
            }
            catch (Exception egc)
            {
                Log.EscribeLog("Existe un error al obtner las configuraciones para el Servicio SCP: " + egc);
            }
            return response;
        }
    }
}
