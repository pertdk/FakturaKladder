using System.Windows;
using EconomicLib.EconomicApi;
using Microsoft.Win32;
using System.Collections.Generic;
using System;
using System.IO;
using LumenWorks.Framework.IO.Csv;

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

        private Dictionary<string, DebtorHandle> Debtors;
        private Dictionary<string, ProductHandle> Products;

        public MainWindow()
        {
            InitializeComponent();
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
            SetStatus("Login succesfull!");
            BtnLogin.IsEnabled = false;
            BtnLogout.IsEnabled = true;
            BtnGet.IsEnabled = true;
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

        private void GetDebtors()
        {
            if (EconomicSession != null)
            {
                Debtors = new Dictionary<string, DebtorHandle>();
                //AccountHandle[] Accounts = EconomicSession.Account_GetAll();
                DebtorHandle[] eDebtors = EconomicSession.Debtor_GetAll();
                string nr = "";
                foreach( DebtorHandle dh in eDebtors)
                {
                    nr = EconomicSession.Debtor_GetNumber(dh);
                    Debtors[nr] = dh;
                }
                MessageBox.Show("Debtors gotten");
            }
            else
            {
                SetStatus("Not logged in!");
            }

        }
        private void GetProducts()
        {
            if ( EconomicSession!=null )
            {
                Products = new Dictionary<string, ProductHandle>();
                IDictionary<string, ProductData> ProductData = new Dictionary<string, ProductData>();
                ProductHandle[] eProducts = EconomicSession.Product_GetAll();
                string nr = "";
                foreach (ProductHandle ph in eProducts)
                {
                    nr = EconomicSession.Product_GetNumber(ph);
                    ProductData pd = EconomicSession.Product_GetData(ph);
                    ProductData[nr] = pd;
                    Products[nr] = ph;
                }
                MessageBox.Show("Products gotten");
            }
            else
            {
                SetStatus("Not logged in!");
            }
        }

        private void CreateInvoice()
        {
            string debtorId = "202565";
            // Get Debtor
            DebtorHandle dh = Debtors[debtorId];
            if (dh != null)
            {
                DateTime dueDate = DateTime.Parse("2016-06-30");
                string debtorName = EconomicSession.Debtor_GetName(dh);
                CurrentInvoiceHandle ci_h = EconomicSession.CurrentInvoice_Create(dh);
                TermOfPaymentHandle term_h = EconomicSession.TermOfPayment_Create("Forfaldsdato", TermOfPaymentType.DueDate, null);
                EconomicSession.CurrentInvoice_SetTermOfPayment(ci_h, term_h);
                EconomicSession.CurrentInvoice_SetDueDate(ci_h, dueDate);
                CurrentInvoiceLineHandle cil_h = EconomicSession.CurrentInvoiceLine_Create(ci_h);
                EconomicSession.CurrentInvoiceLine_SetProduct(cil_h, Products["1"]);
                EconomicSession.CurrentInvoiceLine_SetQuantity(cil_h, 1);
                MessageBox.Show("Invoice created!");
            }
            else
            {
                MessageBox.Show(" Not found ");
            }

        }

        private void BtnGet_Click(object sender, RoutedEventArgs e)
        {
            GetDebtors();
            GetProducts();
            CreateInvoice();
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

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".csv";
            openFileDialog.Filter = "CSV Files (.csv) |*.csv";
            openFileDialog.CheckFileExists = true;
            openFileDialog.Multiselect = false;
            if ( openFileDialog.ShowDialog()==true )
            {
                txtBoxFilename.Text = openFileDialog.FileName;
            }
        }

        private void ReadFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                CsvReader csv = new CsvReader(new StreamReader(fileName, System.Text.Encoding.Default), false, ';');
                csv.MissingFieldAction = MissingFieldAction.ReplaceByEmpty;
                csv.DefaultParseErrorAction = ParseErrorAction.ThrowException;
                int rowCount = 0;
                MessageBox.Show("FieldCount"+csv.FieldCount);
                int kundeNrCol = csv.FieldCount - 8;
                int navnCol = csv.FieldCount - 7;
                string kundeNr = csv[rowCount, kundeNrCol];
                string navn = csv[rowCount, navnCol];
                while (csv.ReadNextRecord())
                {   
                    rowCount++;
                    if (csv[rowCount, kundeNrCol].Trim() != ""
                        && csv[rowCount, navnCol].Trim() != "")
                    {
                        kundeNr = csv[rowCount, kundeNrCol];
                        navn = csv[rowCount, navnCol];
                    }
                }
                MessageBox.Show("Kundenr: " + kundeNr + "\nNavn: " + navn);
            }
        }
        private void BtnReadFile_Click(object sender, RoutedEventArgs e)
        {
            ReadFile(txtBoxFilename.Text);
        }
    }
}
