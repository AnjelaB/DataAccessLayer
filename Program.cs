using System;

namespace DataAccessLayer
{
    class Program
    {
        static void Main(string[] args)
        {
            DataAndObjects<Product> product = new DataAndObjects<Product>();
            product.CreateConnection("Production");
            var products = product.GetData("1", null);
            foreach(var el in products)
            {
                Console.WriteLine(" " + el.Name + " " + el.ProductNumber + " " + el.Price);
            }
            Console.ReadLine();
        }
    }
}
