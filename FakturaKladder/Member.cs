using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EconomicLib.EconomicApi;
using System.Windows;

namespace FakturaKladder
{
    class Member
    {
        public string KundeNr { get; set; }
        public string Navn { get; set; }
        public string Email { get; set; }
        public string KundeGruppe { get; set; }
        public string Antal { get; set; }
        public string Sum { get; set; }
        public string FakturaNr { get; set; }
        public string Betaling { get; set; }


        public bool CreateInvoice( EconomicWebServiceSoapClient session)
        {
            bool created = false;
            if( session!=null)
            {
                DebtorHandle dh = session.Debtor_FindByNumber(KundeNr);
                if (dh != null)
                {
                    session.Debtor_SetEmail(dh, Email);
                    DebtorGroupHandle dgh = session.DebtorGroup_FindByNumber(int.Parse(KundeGruppe));
                    if ( dgh!=null) {
                    session.Debtor_SetDebtorGroup(dh, dgh);
                        ProductHandle ph = session.Product_FindByNumber("1");
                        if (ph != null)
                        {
                            UnitHandle uh = session.Unit_FindByNumber(1);
                            if (uh != null)
                            {

                                DateTime FakturaDato = DateTime.Parse(Betaling);
                                TermOfPaymentHandle[] tohs = session.TermOfPayment_FindByName("Netto 8 dage");
                                TermOfPaymentHandle term_h = tohs[0];

                                CurrentInvoiceHandle ci_h = session.CurrentInvoice_Create(dh);

                                session.CurrentInvoice_SetTermOfPayment(ci_h, term_h);
                                session.CurrentInvoice_SetPublicEntryNumber(ci_h, FakturaNr);
                                session.CurrentInvoice_SetDate(ci_h, FakturaDato);

                                CurrentInvoiceLineHandle cil_h = session.CurrentInvoiceLine_Create(ci_h);
                                session.CurrentInvoiceLine_SetProduct(cil_h, ph);
                                session.CurrentInvoiceLine_SetDescription(cil_h, "Kontingent 2016/17");

                                decimal? price = Decimal.Parse("485,00");
                                decimal ? count = Decimal.Parse(Antal);

                                session.CurrentInvoiceLine_SetUnitNetPrice(cil_h, price);
                                session.CurrentInvoiceLine_SetUnit(cil_h, uh);
                                session.CurrentInvoiceLine_SetQuantity(cil_h, count);



                                created = true;
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Not logged in!");
            }
            return created;
        }
    }
}
