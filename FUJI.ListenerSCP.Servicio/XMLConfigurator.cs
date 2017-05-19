using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FUJI.ListenerSCP.Servicio
{
    public class XMLConfigurator
    {
        public static string path = ConfigurationManager.AppSettings["ConfigDirectory"] != null ? ConfigurationManager.AppSettings["ConfigDirectory"].ToString() : "";
        public static clsConfiguracion getXMLfile()
        {
            clsConfiguracion _config = new clsConfiguracion();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path + "info.xml");

                XmlNode node = doc.DocumentElement.SelectSingleNode("/Configuraciones/sitio");
                //Sitio
                _config.vchClaveSitio = node["claveSitio"]?.InnerText;
                _config.vchNombreSitio = node["NombreSitio"]?.InnerText;
                _config.vchAETitle = node["AETitle"]?.InnerText;
                _config.bitActivo = node["Activo"]?.InnerText == "1" ? true : false;
                //Local
                XmlNode nodeL = doc.DocumentElement.SelectSingleNode("/Configuraciones/sitio/hostLocal");
                _config.vchIPCliente = nodeL["ip"]?.InnerText;
                _config.vchMaskCliente = nodeL["mask"]?.InnerText;
                _config.intPuertoCliente = nodeL["puerto"]?.InnerText != "" ? Convert.ToInt32(nodeL["puerto"]?.InnerText) : 0;

                //Server
                XmlNode nodeS = doc.DocumentElement.SelectSingleNode("/Configuraciones/sitio/hostServer");
                _config.vchIPServidor = nodeS["ip"]?.InnerText;
                _config.intPuertoServer = nodeS["puerto"]?.InnerText != "" ? Convert.ToInt32(nodeS["puerto"]?.InnerText) : 0;

                //Usuario
                XmlNode nodeUser = doc.DocumentElement.SelectSingleNode("/Configuraciones/User");
                _config.intTipoUsuario = nodeUser["tipoUsuario"]?.InnerText != "" ? Convert.ToInt32(nodeUser["tipoUsuario"]?.InnerText) : 0;
                _config.vchNombreUsuario = nodeUser["NombreUser"]?.InnerText;
                _config.vchUsuario = nodeUser["usuario"]?.InnerText;
                _config.vchPassword = nodeUser["Pass"]?.InnerText;
            }
            catch (Exception eXMLC)
            {
                Log.EscribeLog("Existe un error al obtener los valores de configuración: " + eXMLC.Message);
            }
            return _config;

        }

        public static bool setConfiguracionClienteXML(clsConfiguracion _config, ref string mensaje)
        {
            bool valido = false;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path + "info.xml");
                XmlNode node = doc.DocumentElement.SelectSingleNode("/Configuraciones/sitio");
                //Sitio
                node["claveSitio"].InnerText = _config.vchClaveSitio;
                node["claveSitio"].InnerText = _config.vchClaveSitio;
                node["NombreSitio"].InnerText = _config.vchNombreSitio;
                node["AETitle"].InnerText = _config.vchAETitle;
                node["Activo"].InnerText = "1";
                XmlNode nodeL = doc.DocumentElement.SelectSingleNode("/Configuraciones/sitio/hostLocal");
                nodeL["ip"].InnerText = _config.vchIPCliente;
                nodeL["mask"].InnerText = _config.vchMaskCliente;
                nodeL["puerto"].InnerText = _config.intPuertoCliente.ToString();
                doc.Save(path + "info.xml");
                valido = true;
            }
            catch (Exception esC)
            {
                valido = false;
                mensaje = esC.Message;
                Log.EscribeLog("Existe un error al actualizar los datos de configuración. " + esC.Message);
            }
            return valido;
        }

        public static bool setConfiguracionServerXML(clsConfiguracion _config, ref string mensaje)
        {
            bool valido = false;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path + "info.xml");
                //Server
                XmlNode nodeS = doc.DocumentElement.SelectSingleNode("/Configuraciones/sitio/hostServer");
                nodeS["ip"].InnerText = _config.vchIPServidor;
                nodeS["puerto"].InnerText = _config.intPuertoServer.ToString();
                doc.Save(path + "info.xml");
                valido = true;
            }
            catch (Exception esC)
            {
                valido = false;
                mensaje = esC.Message;
                Log.EscribeLog("Existe un error al actualizar los datos de configuración. " + esC.Message);
            }
            return valido;
        }

        public static bool setConfiguracionUsuarioXML(clsConfiguracion _config, ref string mensaje)
        {
            bool valido = false;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path + "info.xml");
                //Usuario
                XmlNode nodeUser = doc.DocumentElement.SelectSingleNode("/Configuraciones/User");
                nodeUser["tipoUsuario"].InnerText = _config.intTipoUsuario.ToString();
                nodeUser["NombreUser"].InnerText = _config.vchNombreUsuario;
                nodeUser["usuario"].InnerText = _config.vchUsuario;
                nodeUser["Pass"].InnerText = _config.vchPassword;
                doc.Save(path + "info.xml");
                valido = true;
            }
            catch (Exception esC)
            {
                valido = false;
                mensaje = esC.Message;
                Log.EscribeLog("Existe un error al actualizar los datos de configuración. " + esC.Message);
            }
            return valido;
        }
    }
}
