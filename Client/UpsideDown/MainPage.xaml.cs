using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

//TODO: actually do something in catches

//TODO: Need to replace all dictionary references with a default-granting TryGetValue 
//such as dictionary.TryGetValue(key, out value) ? value : defaultValueProvider();

//TODO: Write RUN method

//TODO: add CANCEL/ABORT command

//TODO: make the UI pretty


namespace UpsideDown
{
    public sealed partial class MainPage : Page
    {
        #region params
        private CoreDispatcher dispatcher;

        private bool isListening = false;
        private StringBuilder dictatedTextBuilder = new StringBuilder();

        // defaults before device language scanning occurs
        public static Language host_language = new Language("en-US");
        public static string voiceMatchLanguageCode = "en";

        private SpeechRecognizer speechRecognizer;

        private string rootUri = "http://192.168.0.101/";

        private string luisUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/";
        private string AppId = "YOUR APP ID";
        private string key = "YOUR APP KEY";

        Dictionary<string, string> peopleList = new Dictionary<string, string>()
        {
            { "WILL","WILL"},    
            { "WILLIAM","WILL"}, 
            { "WILLY","WILL"},       
            { "WILL BEYERS", "WILL"},
            { "WILL BUYERS", "WILL"},
            { "WILL BIERS", "WILL"}, 
            { "WILL BIRES", "WILL"}, 
            { "WILL BYERS", "WILL"},
            { "YOU", "WILL"},
            { "HOPPER","HOPPER" },
            { "SHERIFF","HOPPER" },
            { "THE SHERIFF","HOPPER" },
            { "SHERIFF HOPPER","HOPPER" },
            { "EL","ELEVEN" },
            { "ELL","ELEVEN" },
            { "ELEVEN","ELEVEN" },
            { "L","ELEVEN" },
            { "11","ELEVEN" },
        };

        public bool IsReadyToRecognize => speechRecognizer != null;
        #endregion

        #region STT
        public MainPage()
        {
            this.InitializeComponent();
            InitRecog();
        }

        public async void InitRecog()
        {
            await InitializeRecognizer(host_language);
        }
        
        private async Task InitializeRecognizer(Language recognizerLanguage, Language speechLanguage = null)
        {
            try
            {
                if (speechRecognizer != null)
                {
                    speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
                    speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
                    speechRecognizer.HypothesisGenerated -= SpeechRecognizer_HypothesisGenerated;
                    speechRecognizer.Dispose();
                    speechRecognizer = null;
                }

                speechRecognizer = new SpeechRecognizer(recognizerLanguage);

                SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
                if (result.Status != SpeechRecognitionResultStatus.Success)
                {
                    checkError.Visibility = Visibility.Visible;
                    errorCheck.Visibility = Visibility.Visible;
                    errorCheck.Text = "Recognition Failed!";
                }

                speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
                speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated;

                isListening = false;
                dispatcher = this.Dispatcher;

                bool permissionGained = await AudioCapturePermissions.RequestMicrophoneCapture();
                if (!permissionGained)
                {
                    this.dictationTextBox.Text = "Requesting Microphone Capture Fails; Make sure Microphone is plugged in";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            try
            {
                if (args.Status != SpeechRecognitionResultStatus.Success)
                {
                    if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                    {
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            await StopRecognition();
                            checkError.Visibility = Visibility.Visible;
                            errorCheck.Visibility = Visibility.Visible;
                            errorCheck.Text = "Automatic Time out of Dictation";
                            dictationTextBox.Text = dictatedTextBuilder.ToString();
                        });
                    }
                    else
                    {
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            await StopRecognition();

                            checkError.Visibility = Visibility.Visible;
                            errorCheck.Visibility = Visibility.Visible;
                            errorCheck.Text = "Continuous Recognition Completed:" + args.Status.ToString();

                            buildResponse(dictationTextBox.Text);
                        });
                    }
                }
                else await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    buildResponse(dictationTextBox.Text);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            try
            {
                string hypothesis = args.Hypothesis.Text;

                // Update the textbox with the currently confirmed text, and the hypothesis combined.
                string textboxContent = dictatedTextBuilder.ToString() + " " + hypothesis + " ...";

                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    dictationTextBox.Text = textboxContent;
                    btnClearText.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            try
            {
                string s = args.Result.Text;

                if (args.Result.Status == SpeechRecognitionResultStatus.Success)
                {
                    dictatedTextBuilder.Append(s + " ");
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        dictationTextBox.Text = dictatedTextBuilder.ToString();
                        btnClearText.IsEnabled = true;
                    });
                }
                else
                {
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        dictationTextBox.Text = dictatedTextBuilder.ToString();
                    });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void btnStartTalk_Click(object sender = null, RoutedEventArgs e = null)
        {
            try
            {
                if (!isListening)
                {
                    await StartRecognition();
                }
                else
                {
                    await StopRecognition();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task StartRecognition()
        {
            try
            {
                if (speechRecognizer.State == SpeechRecognizerState.Idle)
                {
                    errorCheck.Text = string.Empty;

                    StartTalkButtonText.Text = "Stop Talk";
                    try
                    {
                        isListening = true;
                        await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                        await messageDialog.ShowAsync();

                        isListening = false;
                        StartTalkButtonText.Text = "Start Talk";
                    }
                }
            }
            catch(Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task StopRecognition()
        {
            try
            {
                isListening = false;
                StartTalkButtonText.Text = "Start Talk";
                if (speechRecognizer.State != SpeechRecognizerState.Idle)
                {
                    await speechRecognizer.ContinuousRecognitionSession.StopAsync();
                    dictationTextBox.Text = dictatedTextBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void btnClearText_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnClearText.IsEnabled = false;
                dictatedTextBuilder.Clear();
                dictationTextBox.Text = "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region Response
        private async void buildResponse(string userQuery)
        {
            try
            {
                var client = new HttpClient();
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                queryString["q"] = userQuery;
                var endpointUri = luisUri + AppId + "?" + queryString;

                var response = await client.GetAsync(endpointUri);
                var jsonStream = await response.Content.ReadAsStreamAsync();

                var ser = new DataContractJsonSerializer(typeof(luisResponse));
                luisResponse luisResponse = ser.ReadObject(jsonStream) as luisResponse;
                jsonStream.Close();

                switch (luisResponse.topScoringIntent.intent)
                {
                    case "Locate":
                        intentLocate(luisResponse);
                        break;
                    case "greet":
                        intentGreet(luisResponse);
                        break;
                    case "None":
                        intentNone(luisResponse);
                        break;
                }
                //call child methods for intent branching
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void intentNone(luisResponse luisResponse)
        {
            callLights("RUN");
        }

        private void intentGreet(luisResponse luisResponse)
        {
            try
            {
                if (luisResponse.entities.Count > 0)
                {
                    var hasName = false;
                    foreach (var ent in luisResponse.entities)
                    {
                        if (ent.type == "Name")
                        {
                            hasName = true;
                            if (peopleList[ent.entity.ToUpper()] == "WILL")
                                callLights("HI");
                            else callLights("NOT_" + peopleList[ent.entity.ToUpper()]);
                        }
                    }
                }
                else callLights("HI");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void intentLocate(luisResponse luisResponse)
        {
            try
            {
                if (luisResponse.entities.Count > 0)
                {
                    var hasName = false;
                    foreach (var ent in luisResponse.entities)
                    {
                        if (ent.type == "Name")
                        {
                            hasName = true;
                            switch (peopleList[ent.entity.ToUpper()])
                            {
                                case "WILL":
                                    callLights("HERE");
                                    break;
                                case "HOPPER":
                                    callLights("UPSIDE_DOWN");
                                    break;
                                case "ELEVEN":
                                    callLights("KICKING_ASS");
                                    break;
                            }
                        }
                    }
                }
                else callLights("HERE");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void callLights(string reply)
        {
            try
            {
                //GET page with string
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(rootUri + reply);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Above three lines can be replaced with new helper method below
                    // string responseBody = await client.GetStringAsync(uri);

                    Console.WriteLine(responseBody);
                }


            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion
    }

    #region Deserialization
    [DataContract]
    class luisResponse
    {
        [DataMember]
        public string query { get; set; }
        [DataMember]
        public topScoringIntent topScoringIntent { get; set; }
        [DataMember]
        public List<entities> entities { get; set; }
    }
    [DataContract]
    class topScoringIntent
    {
        [DataMember]
        public string intent { get; set; }
        [DataMember]
        public decimal score { get; set; }
    }
    [DataContract]
    class entities
    {
        [DataMember]
        public string entity { get; set; }
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public int startIndex { get; set; }
        [DataMember]
        public int endIndex { get; set; }
        [DataMember]
        public decimal score { get; set; }
    }
    #endregion
}