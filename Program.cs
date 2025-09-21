using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace RalarsaPrueba1_CSharp
{
    internal class Program
    {
        private const string ElPaisWebsiteURL = "https://www.elpais.com";
        private const string NombreFicheroSalida = "elPaisTag.txt";
        private static readonly By SubscriptionButton = By.Id("s_b_df");//id del botón de suscripción
        private static readonly By ModalCookies = By.Id("pmConsentWall");
        private static readonly By FirstAnchor = By.TagName("a");
        static void Main(string[] args)
        {
            //DriverManager configura una versión de ChromeDriver compatible con la versión de Chrome instalada
            string chromeDriverConfig = new DriverManager().SetUpDriver(new ChromeConfig());
            Console.WriteLine("--Ubicación de ChromeDriver: " + chromeDriverConfig);

            //Opciones para ChromeDriver
            ChromeOptions options = new();
            options.AddArgument("--log-level=3");
            options.AddArgument("--headless"); //a gusto personal, no abre ventana. Mejora rendimiento

            using ChromeDriver driver = new(options);

            try
            {                
                driver.Navigate().GoToUrl(ElPaisWebsiteURL);

                //Se aplican 10 segundos de timeout, si no encuentra el elemento en ese tiempo, lanza Exception
                WebDriverWait driverWaiter = new(driver, TimeSpan.FromSeconds(10));

                //Esperar a que el JavaScript se cargue totalmente
                WaitForJavascript(driver, driverWaiter);

                //Aceptar las cookies
                AcceptCookies(driver, driverWaiter);

                // Clickar el botón de suscripción
                ClickSubscriptionButton(driverWaiter);

                //Conseguir el primer tag <a> y guardarlo en el fichero
                GetFirstATag(driverWaiter);


            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inesperado -> " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Fin del programa.");

                Console.WriteLine("Haz click en Enter para salir del programa.");
                Console.ReadLine();
            }
        }

        static void WaitForJavascript(ChromeDriver driver, WebDriverWait driverWaiter)
        {

            Console.WriteLine("Cargando JavaScript...");

            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            driverWaiter.Until(d => js.ExecuteScript("return document.readyState").ToString() == "complete");

            Console.WriteLine("Hecho.");
        }

        static void AcceptCookies(IWebDriver driver, WebDriverWait driverWaiter)
        {
            Console.WriteLine("Aceptando Cookies...");

            // Esperar a que abra al modal de cookies
            IWebElement modal = driverWaiter.Until(d => d.FindElement(ModalCookies));

            //El botón aceptar no tiene id, buscar por la clase CSS (o en su defecto, confiar en el orden de los botones y elegir el primero)
            //Buscar el primer <button> con clase pmConsentWall-button (aceptar cookies)
            //El otro tag con la misma clase (rechazar y suscribirse) es un <a>.
            IWebElement botonAceptarCookies = modal.FindElement(By.CssSelector($"button.pmConsentWall-button"));

            botonAceptarCookies.Click();

            // Esperar a que se cierre el modal de cookies
            driverWaiter.Until(d =>
            {
                try
                {
                    return !d.FindElement(ModalCookies).Displayed;
                }
                catch (NoSuchElementException)
                {
                    return true; //Ya no se encuentra el modal, salir
                }
            });

            Console.WriteLine("Hecho.");
        }

        static void ClickSubscriptionButton(WebDriverWait driverWaiter)
        {
            Console.WriteLine("Buscando botón de suscripción...");

            //Busca el botón de suscripción hasta encontrarlo en los 10 segundos de timeout, sinó lanza Exception
            driverWaiter.Until(d =>
            {
                try
                {
                    var boton = d.FindElement(SubscriptionButton);
                    boton.Click();
                    return true;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });

            Console.WriteLine("Hecho.");

        }

        static void GetFirstATag(WebDriverWait driverWaiter)
        {
            Console.WriteLine("Obteninendo primer <a>...");

            IWebElement primerTagA = driverWaiter.Until(d =>
            {
                try
                {
                    IWebElement elemento = d.FindElement(FirstAnchor);
                    //return elemento.Displayed ? elemento : null; //opción para no considerar elementos ocultos, ignorado por no pedirlo en el enunciado
                    return elemento;
                }
                catch (StaleElementReferenceException)
                {
                    return null; // volver a intentar hasta que el elemento sea válido
                }
            });

            Console.WriteLine("Hecho.");

            string htmlTagA = primerTagA.GetAttribute("outerHTML");

            string rutaCompletaSalida = Path.Combine(AppContext.BaseDirectory, NombreFicheroSalida);
            using (StreamWriter sw = new(rutaCompletaSalida))
            {
                sw.WriteLine(htmlTagA);
            }
            Console.WriteLine($"Archivo guardado en: {rutaCompletaSalida}");

        }
    }
}
