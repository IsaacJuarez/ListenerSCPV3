using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUJI.ListenerSCP.Servicio
{
    public class clsConfiguracion
    {
        public int id_Sitio { get; set; }
        public string vchClaveSitio { get; set; }
        public string vchNombreSitio { get; set; }
        public string vchIPCliente { get; set; }
        public string vchMaskCliente { get; set; }
        public int intPuertoCliente { get; set; }
        public string vchIPServidor { get; set; }
        public int intPuertoServer { get; set; }
        public DateTime datFechaSistema { get; set; }
        public int vchUserChanges { get; set; }
        public string vchAETitle { get; set; }
        public bool bitActivo { get; set; }
        public int intTipoUsuario { get; set; }
        public string vchNombreUsuario { get; set; }
        public string vchUsuario { get; set; }
        public string vchPassword { get; set; }
        public string vchPathLocal { get; set; }


        public clsConfiguracion()
        {
            id_Sitio = int.MinValue;
            vchClaveSitio = string.Empty;
            vchNombreSitio = string.Empty;
            vchIPCliente = string.Empty;
            vchMaskCliente = string.Empty;
            intPuertoCliente = int.MinValue;
            vchIPCliente = string.Empty;
            intPuertoServer = int.MinValue;
            vchAETitle = string.Empty;
            bitActivo = false;
            datFechaSistema = DateTime.MinValue;
            vchUserChanges = int.MinValue;
            intTipoUsuario = int.MinValue;
            vchNombreUsuario = string.Empty;
            vchUsuario = string.Empty;
            vchPassword = string.Empty;
            vchPathLocal = string.Empty;
        }
    }
}
