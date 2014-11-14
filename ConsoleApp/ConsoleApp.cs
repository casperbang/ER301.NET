using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BangBits.ER301.Driver;
using System.Threading;

namespace BangBits.ER301.ConsoleApp
{
    class ConsoleApp : MifareReadCallback
    {

        static byte[] KEY_GENERIC_FF = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };

        static void Main(string[] args)
        {
            new ConsoleApp();
        }

        public ConsoleApp()
        {
            using (Mifare mifare = new Mifare("com3", this)) { };

            Console.ReadKey();
        }

        public byte[] getKeyABySector(int sectorIndex)
        {
            return KEY_GENERIC_FF;
        }

        public byte[] getKeyBBySector(int sectorId)
        {
            throw new NotImplementedException();
        }

        public bool getAuthByKeyA()
        {
            return true;
        }

        public bool getAuthByKeyB()
        {
            return false;
        }

        public bool getInclKeys()
        {
            return false;
        }

        public void status(string serialNo)
        {
            Console.WriteLine(serialNo);
        }

        public void completeBlock(int blockIndex, byte[] data)
        {
            Console.WriteLine(blockIndex + ": " + data.ToHex() );
        }

        public void success(uint serialNoHex, string sha1Hash)
        {
            Console.WriteLine(serialNoHex);
        }

        public void error()
        {
            Console.WriteLine("Unknown error!");
        }
    }
}
