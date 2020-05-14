﻿using System;
using System.Collections.Generic;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Models.Mailing
{
    public readonly struct BookingInvoiceData
    {
        public BookingInvoiceData(int id, in BuyerInfo buyerDetails, in SellerInfo sellerDetails, string referenceCode,
            List<InvoiceItemInfo> invoiceItems, MoneyAmount totalPrice, in DateTime invoiceDate, in DateTime payDueDate)
        {
            Id = id;
            BuyerDetails = buyerDetails;
            InvoiceDate = invoiceDate;
            PayDueDate = payDueDate;
            ReferenceCode = referenceCode;
            InvoiceItems = invoiceItems;
            TotalPrice = totalPrice;
            SellerDetails = sellerDetails;
        }


        public int Id { get; }
        public BuyerInfo BuyerDetails { get; }
        public DateTime InvoiceDate { get; }
        public DateTime PayDueDate { get; }
        public string ReferenceCode { get; }
        public List<InvoiceItemInfo> InvoiceItems { get; }
        public MoneyAmount TotalPrice { get; }
        public SellerInfo SellerDetails { get; }


        public readonly struct BuyerInfo
        {
            public BuyerInfo(string name, string address, string contactPhone, string email)
            {
                Address = address;
                ContactPhone = contactPhone;
                Email = email;
                Name = name;
            }


            public string Address { get; }
            public string ContactPhone { get; }
            public string Email { get; }
            public string Name { get; }
        }


        public readonly struct SellerInfo
        {
            public SellerInfo(string companyName, string bankName, string bankAddress, string accountNumber, string iban, string routingCode, string swiftCode)
            {
                AccountNumber = accountNumber;
                BankAddress = bankAddress;
                BankName = bankName;
                CompanyName = companyName;
                Iban = iban;
                RoutingCode = routingCode;
                SwiftCode = swiftCode;
            }


            public string AccountNumber { get; }
            public string BankAddress { get; }
            public string BankName { get; }
            public string CompanyName { get; }
            public string Iban { get; }
            public string RoutingCode { get; }
            public string SwiftCode { get; }
        }
        
        
        public readonly struct InvoiceItemInfo
        {
            public InvoiceItemInfo(int number, string accommodationName, string roomDescription, MoneyAmount price, MoneyAmount total)
            {
                Number = number;
                AccommodationName = accommodationName;
                RoomDescription = roomDescription;
                Price = price;
                Total = total;
            }
            
            public int Number { get; }
            public string AccommodationName { get; }
            public string RoomDescription { get; }
            public MoneyAmount Price { get; }
            public MoneyAmount Total { get; }
        }
    }
}