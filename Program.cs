using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace RalarsaPrueba1_CSharp
{
    internal class Program
    {
        const string ElPaisWebsiteURL = "https://www.elpais.com";
        const string SubscriptionHtmlTagId = "s_b_df"; //id del botón de suscripción
        const string ModalCookiesHtmlTagId = "pmConsentWall"; //id del modal de cookies
        const string NombreFicheroSalida = "elPaisTag.txt";

        static void Main(string[] args)
        {
            //DriverManager configura una versión de ChromeDriver compatible con la versión de Chrome instalada
            string chromeDriverConfig = new DriverManager().SetUpDriver(new ChromeConfig());
            Console.WriteLine("--Ubicación de ChromeDriver: " + chromeDriverConfig);

            //Opciones para ChromeDriver
            ChromeOptions options = new();
            options.AddArgument("--log-level=3");
            options.AddArgument("--headless=new"); //a gusto personal, no abre ventana. Mejora rendimiento

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

                driver.Quit();

                Console.WriteLine("Haz click en Enter para salir del programa.");
                Console.ReadLine();
            }
        }

        static void WaitForJavascript(ChromeDriver driver, WebDriverWait driverWaiter)
        {
            //Espera a que el JavaScript se cargue totalmente
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            driverWaiter.Until(d => js.ExecuteScript("return document.readyState").ToString() == "complete");
        }

        static void AcceptCookies(IWebDriver driver, WebDriverWait driverWaiter)
        {
            // Esperar a que abra al modal de cookies
            IWebElement modal = driverWaiter.Until(d => d.FindElement(By.Id(ModalCookiesHtmlTagId)));

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
                    return !d.FindElement(By.Id(ModalCookiesHtmlTagId)).Displayed;
                }
                catch (NoSuchElementException)
                {
                    return true; //Ya no se encuentra el modal, salir
                }
            });
        }

        static void ClickSubscriptionButton(WebDriverWait driverWaiter)
        {
            //Busca el botón de suscripción hasta encontrarlo en los 10 segundos de timeout, sinó lanza Exception
            driverWaiter.Until(d =>
            {
                try
                {
                    var boton = d.FindElement(By.Id(SubscriptionHtmlTagId));
                    boton.Click();
                    return true;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });

        }

        static void GetFirstATag(WebDriverWait driverWaiter)
        {
            IWebElement primerTagA = driverWaiter.Until(d =>
            {
                try
                {
                    IWebElement elemento = d.FindElement(By.TagName("a"));
                    //return elemento.Displayed ? elemento : null; //opción para no considerar elementos ocultos, ignorado por no pedirlo en el enunciado
                    return elemento;
                }
                catch (StaleElementReferenceException)
                {
                    return null; // volver a intentar hasta que el elemento sea válido
                }
            });

            string htmlTagA = primerTagA.GetAttribute("outerHTML");

            using (StreamWriter sw = new(NombreFicheroSalida))
            {
                sw.WriteLine(htmlTagA);
            }
            Console.WriteLine($"Archivo guardado en: {Path.GetFullPath(NombreFicheroSalida)}");

        }
    }
}
