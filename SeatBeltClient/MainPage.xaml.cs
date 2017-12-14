using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SeatBeltClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string ServerUrl = "http://91.134.196.77:8101/";
        private const string SeatBeltUrl = ServerUrl + "seatbelt";
        private const string SeatBeltId = "5a180c10857aba0001b937f4";

        private const int BUTTON_PIN = 5;
        private const int LED_PIN = 6;
        private GpioPin ledPin;
        private GpioPin buttonPin;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        private bool beltConnected;
        public bool BeltConnected
        {
            get { return beltConnected; }
            set
            {
                beltConnected = value;
                if (beltConnected)
                {
                    if (ledPin != null)
                    {
                        ledPin.Write(GpioPinValue.High);
                    }
                    LED.Fill = redBrush;
                    BeltStatus.Text = "Secured";
                }
                else
                {
                    if (ledPin != null)
                    {
                        ledPin.Write(GpioPinValue.Low);
                    }
                    LED.Fill = grayBrush;
                    BeltStatus.Text = "Not Secured";
                }
            }
        }

        public MainPage()
        {
            InitializeComponent();

            try
            {
                InitGPIO();
            }
            catch (Exception)
            {
                GpioStatus.Text += "There is no GPIO controller on this device.\n";
            }

            InitSeatBelt();
        }

        private async void InitSeatBelt()
        {
            try
            {
                await SetSeatBeltStatusAsync(SeatBeltId, false);
                SeatBeltGET sb = await GetSeatBeltStatusAsnyc(SeatBeltId);
                BeltConnected = sb.secured;
                GpioStatus.Text += "Connected to remote.\n";
            }
            catch (Exception)
            {
                GpioStatus.Text += "Remote connection failed!\n";
                BeltConnected = false;
            }
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                ledPin = null;
                buttonPin = null;
                GpioStatus.Text = "There is no GPIO controller on this device.\n";
                return;
            }
            else
            {
                GpioStatus.Text = "GPIO controller found. Prepare for take-off!\n";
            }

            ledPin = gpio.OpenPin(LED_PIN);
            buttonPin = gpio.OpenPin(BUTTON_PIN);

            ledPin.SetDriveMode(GpioPinDriveMode.Output);

            // Check if input pull-up resistors are supported
            if (buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
            {
                buttonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            }
            else
            {
                buttonPin.SetDriveMode(GpioPinDriveMode.Input);
            }
            // Set a debounce timeout to filter out switch bounce noise from a button press
            buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);

            // Register for the ValueChanged event so our buttonPin_ValueChanged 
            // function is called when the button is pressed
            buttonPin.ValueChanged += buttonPin_ValueChanged;
        }

        private async void buttonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                // need to invoke UI updates on the UI thread because this event
                // handler gets invoked on a separate thread.
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // toggle the state of the LED every time the button is pressed
                    BeltConnected = true;
                });

                try
                {
                    SetSeatBeltStatusAsync(SeatBeltId, true);
                }
                catch (Exception)
                {
                    // nothing to do
                }
            }

            if (e.Edge == GpioPinEdge.RisingEdge)
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    BeltConnected = false;
                });

                try
                {
                    SetSeatBeltStatusAsync(SeatBeltId, false);
                }
                catch (Exception)
                {
                    // nothing to do
                }
            }
        }

        private async Task<T> GetRequestAsnyc<T>(string uri)
        {
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(new Uri(uri));
                T result = JsonConvert.DeserializeObject<T>(json);
                return result;
            }
        }

        private async Task PostRequestAsnyc(string uri, object payload)
        {
            using (var client = new HttpClient())
            {
                string payloadJson = JsonConvert.SerializeObject(payload);
                await client.PostAsync(new Uri(uri), new StringContent(payloadJson, new UTF8Encoding(), "application/json"));
            }
        }

        public async Task<SeatBeltGET> GetSeatBeltStatusAsnyc(string id)
        {
            string query = SeatBeltUrl + "?id=" + id;
            /*var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                GpioStatus.Text += query + "\n";
            });*/
            return await GetRequestAsnyc<SeatBeltGET>(query);
        }

        public async Task SetSeatBeltStatusAsync(string id, bool secured)
        {
            string query = SeatBeltUrl + "?id=" + id;
            /*var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                GpioStatus.Text += query + "\n";
            });*/

            await PostRequestAsnyc(query, new SeatBeltPOST()
            {
                secured = secured
            });
        }
    }
}
