using System;
using System.Collections.Generic;

namespace BankAccounts.Models
{
    public class IndexWrapper
    {
        public List<Transaction> Transactions {get;set;}
        public Transaction Transaction {get;set;}

    }
}