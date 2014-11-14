using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BangBits.ER301.Driver
{
    /// <summary>
    /// Communication between host application and the ER301 is done though this callback.
    /// The host application must implement the methods according to the specific needs.
    /// There *must* always be at least one auth key, even if it is a default 0-based one.
    /// 
    /// </summary>
    public interface MifareReadCallback
    {
        /// <summary>
        /// When the ER301 tried to read a block belonging to a sector which has not
        /// previously been authorized, it asks the host for the proper key based on
        /// sectorId.
        /// </summary>
        /// <param name="sectorId"></param>
        /// <returns></returns>
        byte[] getKeyABySector (int sectorId);

        /// <summary>
        /// When the ER301 tried to read a block belonging to a sector which has not
        /// previously been authorized, it asks the host for the proper key based on
        /// sectorId.
        /// </summary>
        /// <param name="sectorId"></param>
        /// <returns></returns>
        byte[] getKeyBBySector(int sectorId);
        
        /// <summary>
        /// Decides whether authorization is done through key A. Often this is the default key
        /// for read operations.
        /// </summary>
        /// <returns></returns>
        bool getAuthByKeyA();

        /// <summary>
        /// Decides whether authorization is done through key B. Often this is the default key
        /// for write operations.
        /// </summary>
        /// <returns></returns>        
        bool getAuthByKeyB();

        /// <summary>
        /// Whether to include key data in the data returned through the completeBlock callback.
        /// 
        /// Note that the ER301 reader and Mifare standard does not allow for the key 
        /// data can be read directly, for security reasons. In stead one must indirectly
        /// verify the key data by attempting to read block data based on both keys.
        /// </summary>
        /// <returns></returns>
        bool getInclKeys();

        /// <summary>
        /// Data callback with serialNo of card
        /// </summary>
        /// <param name="serialNo"></param>
        void status(string serialNo);
        
        /// <summary>
        /// Data callback with blockIndex and 16byte block data
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="data"></param>
        void completeBlock(int blockIndex, byte[] data);
        
        /// <summary>
        /// Data callback with serialNo and sha1hash
        /// </summary>
        /// <param name="serialNoHex"></param>
        /// <param name="sha1Hash"></param>
        void success(uint serialNoHex, string sha1Hash);
        
        /// <summary>
        /// Data callback signaling an error has occurred
        /// </summary>
        void error();
    }
}
