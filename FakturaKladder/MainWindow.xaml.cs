using System.Windows;
using EconomicLib.EconomicApi;
using Microsoft.Win32;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;

namespace FakturaKladder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private const string AgreementGrantToken = "sdKqJlUWOFc1iaE6tO3qHiMp81ACGox1KPCELHes8F81";
        //private const string AppSecretToken = "VoYkXDo7eTZPEHwBSjZuZhKK19EhVKLYajlPcfHyoMQ1";
        private string AppSecretToken { get; set; }
        private string AgreementGrantToken { get; set; }

        private EconomicWebServiceSoapClient EconomicSession;
        private string EconomicConnection;


        private Dictionary<string, Member> Medlemmer;
        private Dictionary<string, Member> Kassererer;

        private Dictionary<string, Member> manglendeMedlemmer;
        private Dictionary<string, Member> manglendeKassererer;


        public MainWindow()
        {
            InitializeComponent();
        }

        private string ReadJsonFile( string Filename )
        {
            string json = string.Empty;
            if (File.Exists(Filename))
            {
                json = File.ReadAllText(Filename);
            }
            else
            {
                MessageBox.Show("No such file: " + Filename);
            }
            return json;
        }
        private void WriteJsonFile( string Filename, string json )
        {
            File.WriteAllText(Filename, json);
        }
        private string EncodeJson(Dictionary<string, Member> dict)
        {
            string json;
            json = JsonConvert.SerializeObject(dict);
            return json;
        }

        private void ParseJson( out Dictionary<string, Member> dict, string json )
        {
            dict = JsonConvert.DeserializeObject<Dictionary<string, Member>>(json);
            foreach( KeyValuePair<string, Member> kv in dict)
            {
                (kv.Value).KundeNr = kv.Key;
            }
        }

        private void SetStatus(string status)
        {
            LblStatusBar.Content = status;
            StatusBarMain.UpdateLayout();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }
        // 884
        // 946
        private void Login()
        {
            EconomicSession = new EconomicWebServiceSoapClient();
            AgreementGrantToken = txtBoxAgreementGrantToken.Text;
            AppSecretToken = Properties.Settings.Default.AppSecretToken;
            EconomicConnection = EconomicSession.ConnectWithToken(AgreementGrantToken, AppSecretToken);
            CompanyHandle company = EconomicSession.Company_Get();
            string agreementNo = company.Number;
            txtBoxAftaleNummer.Text = agreementNo;
            string companyName = EconomicSession.Company_GetName(company);
            txtBoxFirmanavn.Text = companyName;
            BtnLogin.IsEnabled = false;
            BtnLogout.IsEnabled = true;
            SetStatus("Login succesfull!");
        }
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void Logout()
        {
            if (EconomicSession != null)
            {
                EconomicSession.Disconnect();
                EconomicSession = null;
                BtnLogin.IsEnabled = true;
                BtnLogout.IsEnabled = false;
                SetStatus("Logout succesfull!");
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Properties.Settings.Default.Save();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                e.Cancel = true;
            } finally
            {
                Logout();
            }
        }

        private void btnReadJson_Click(object sender, RoutedEventArgs e)
        {
            string MedlemmerFilename = "..\\..\\Medlemmer.json";
            string KasserererFilename = "..\\..\\Kassererer.json";
            string MedlemmerJson = ReadJsonFile(MedlemmerFilename);
            string KasserererJson = ReadJsonFile(KasserererFilename);
            ParseJson(out Medlemmer, MedlemmerJson);
            ParseJson(out Kassererer, KasserererJson);
            MessageBox.Show("Read " + Medlemmer.Count + " members, and " + Kassererer.Count + " cashiers");

        }

        private void btnCreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            int antalMedlemFakturaer = 0;
            int antalKassererFakturaer = 0;
            manglendeMedlemmer = new Dictionary<string, Member>(Medlemmer.Count);
            manglendeKassererer = new Dictionary<string, Member>(Kassererer.Count);

            foreach( KeyValuePair<string,Member> kv in Medlemmer)
            {
                if( kv.Value.CreateInvoice(EconomicSession) )
                {
                    antalMedlemFakturaer++;
                }
                else
                {
                    if ( !manglendeMedlemmer.ContainsKey( kv.Key ) )
                    {
                        manglendeMedlemmer[kv.Key] = kv.Value;
                    }
                }
            }
            MessageBox.Show("Created " + antalMedlemFakturaer + " member invoices");
            string manglendeMedlemmerJson = EncodeJson(manglendeMedlemmer);

            foreach (KeyValuePair<string, Member> kv in Kassererer)
            {
                if (kv.Value.CreateInvoice(EconomicSession))
                {
                    antalKassererFakturaer++;
                }
                else
                {
                    if (!manglendeKassererer.ContainsKey(kv.Key) )
                    {
                        manglendeKassererer[kv.Key] = kv.Value;
                    }
                }
            }
            MessageBox.Show("Created " + antalKassererFakturaer+ " cashier invoices");

        }

        private void btnWriteJson_Click(object sender, RoutedEventArgs e)
        {
            string MedlemmerFilename = "..\\..\\ManglendeMedlemmer.json";
            string KasserererFilename = "..\\..\\ManglendeKassererer.json";

            string MedlemmerJson = EncodeJson(manglendeMedlemmer);
            string KasserererJson = EncodeJson(manglendeKassererer);

            WriteJsonFile(MedlemmerFilename, MedlemmerJson);
            WriteJsonFile(KasserererFilename, KasserererJson);
            MessageBox.Show("Wrote " + manglendeMedlemmer.Count + " members, and " + manglendeKassererer.Count + " cashiers");
        }
    }
}
