using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace Nokk
{
    public static class CurrencyConverter
    {
        public static Currency GetRates(string currencyBase, double value, string[] symbols)
        {
                WebRequest req = WebRequest.Create($@"https://api.exchangeratesapi.io/latest?base={currencyBase}&symbols={symbols[0].ToUpper()},{symbols[1].ToUpper()}");

                string str = "";
                Currency currency;

                WebResponse response = req.GetResponse();
                using (Stream s = response.GetResponseStream()) //Пишем в поток.
                {
                    using (StreamReader r = new StreamReader(s)) //Читаем из потока.
                    {
                        str = r.ReadToEnd();
                    }
                }
                response.Close(); //Закрываем поток
                currency = JsonConvert.DeserializeObject<Currency>(str);
                return currency;
        }
    }
    public class rates
    {
        public double USD { get; set; }
        public double EUR { get; set; }
        public double RUB { get; set; }
    }
    public class Currency
    {
        public rates rates { get; set; }
    }
}
