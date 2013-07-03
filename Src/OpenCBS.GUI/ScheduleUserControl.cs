﻿using System;
using System.Drawing;
using System.Globalization;
using BrightIdeasSoftware;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.Shared;

namespace OpenCBS.GUI
{
    public partial class ScheduleUserControl : System.Windows.Forms.UserControl
    {
        private string _amountFormatString;

        public ScheduleUserControl()
        {
            InitializeComponent();
            Load += (sender, args) => Setup();
            scheduleObjectListView.RowFormatter = FormatRow;
            scheduleObjectListView.CellEditStarting += new CellEditEventHandler(this.HandleCellEditStarting);
            scheduleObjectListView.CellEditFinishing+=new CellEditEventHandler(this.HandleCellEditFinishing);
        }

        public void SetScheduleFor(Loan loan)
        {
            _amountFormatString = loan.UseCents ? "N2" : "N0";
            Setup();
            scheduleObjectListView.SetObjects(loan.InstallmentList);
        }

        private void Setup()
        {
            dateColumn.AspectToStringConverter =
                paymentDateColumn.AspectToStringConverter = value =>
                    {
                        var date = (DateTime?) value;
                        return date.HasValue ? date.Value.ToString("dd.MM.yyyy") : string.Empty;
                    };
            var numberFormatInfo = new NumberFormatInfo
                {
                    NumberGroupSeparator = " ",
                    NumberDecimalSeparator = ",",
                };
            principalColumn.AspectToStringConverter =
                interestColumn.AspectToStringConverter =
                paidPrincipalColumn.AspectToStringConverter =
                paidInterestColumn.AspectToStringConverter =
                totalColumn.AspectToStringConverter =
                olbColumn.AspectToStringConverter = value =>
                    {
                        var amount = (OCurrency) value;
                        return amount.Value.ToString(_amountFormatString, numberFormatInfo);
                    };
            scheduleObjectListView.CellEditActivation = ObjectListView.CellEditActivateMode.DoubleClick;
        }

        private static void FormatRow(OLVListItem item)
        {
            var installment = (Installment)item.RowObject;
            if (installment == null) return;
            if (installment.IsPending) item.BackColor = Color.Orange;
            if (installment.IsRepaid) item.BackColor = Color.FromArgb(61, 153, 57);
            if (installment.IsPending || installment.IsRepaid) item.ForeColor = Color.White;
        }

        private bool CheckValue(CellEditEventArgs e)
        {
            if (e.Column == dateColumn)
            {
                DateTime newDate = Convert.ToDateTime(e.NewValue);
                DateTime prevousInstallmentDate;
                DateTime.TryParse(
                    scheduleObjectListView.GetItem(e.ListViewItem.Index - 1).GetSubItem(dateColumn.Index).ToString(),
                    out prevousInstallmentDate);
                if (newDate < prevousInstallmentDate)
                    return false;
            }
            return true;
        }

        private void HandleCellEditStarting(object sender, CellEditEventArgs e)
        {
            var installment = (Installment)e.RowObject;
            if (installment.IsRepaid)
                e.Cancel = true;
        }

        private void HandleCellEditFinishing(object sender, CellEditEventArgs e)
        {
            if (!CheckValue(e))
                e.Cancel = true;
            if (e.Column == interestColumn)
            {
                var installment = (Installment)e.RowObject;
                decimal amount;
                if (decimal.TryParse(e.NewValue.ToString(), out amount))
                    installment.InterestsRepayment = amount;
                scheduleObjectListView.RefreshObject(e.RowObject);
            }
            if (e.Column == principalColumn)
            {
                var installment = (Installment)e.RowObject;
                decimal amount;
                if (decimal.TryParse(e.NewValue.ToString(), out amount))
                    installment.CapitalRepayment = amount;
                scheduleObjectListView.RefreshObject(e.RowObject);
            }
        }
    }
}
