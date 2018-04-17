using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace FSFB2_structure
{
    public static class ERRORS {
        /// <summary>
        ///  This class contrain all the error constants for the FSFB2_structure namespace
        /// </summary>
        public const int INVALID_CCF = -5;
        public const int INVALID_HCF = -4;
        public const int INVALID_NAME = -3;
        public const int INVALID_RXTX = -2;
        public const int INVALID_INDEX = -1;
        public const int NO_ERROR = 0;
    }
    
    public class FSFB2DataFlow
    /// <summary>
    /// This class contains all the useful info for an FSFB2 Data Flow
    /// /// </summary>
    {
        public string IPAddressRed { get; set; } // IP address of the SIO on Red network
        public string IPAddressBlue { get; set; } // IP address of the SIO on Blue network
        private string NameNode; // Name of the FSFB2 Node that is communicating with
        private Dictionary<string, int> mappingTX; // mapping between name and index (starting from 0) in the TX BSD
        private Dictionary<string, int> mappingRX; // mapping between name and index (starting from 0) in the RX BSD
        public int Subnet { get; set; }
        public int SCAddress { get; set; } // FSFB2 Address of the Safety Computer (ZC or VOBC)
        public int SIOAddress { get; set; } // FSFB2 Address of the SIO

        public FSFB2DataFlow() {
            mappingRX = new Dictionary<string, int>(); // RX mapping (from SC perspective)
            mappingTX = new Dictionary<string, int>(); // TX mapping (from SC perspective)
        }

        public string[] getBitMapping(string RXTX) {
            string[] returnBitmapping;
            if (RXTX == "RX") {
                returnBitmapping = new string[mappingRX.Count];
                foreach (KeyValuePair<string, int> entry in mappingRX) {
                    returnBitmapping[entry.Value] = entry.Key;
                }
            }
            else if (RXTX == "TX") {
                returnBitmapping = new string[mappingTX.Count];
                foreach (KeyValuePair<string, int> entry in mappingTX) {
                    returnBitmapping[entry.Value] = entry.Key;
                }
            }
            else returnBitmapping = null;
            return returnBitmapping;
        }

        public int SetNameIndex(string name, int index, string RXTX) {
            if (index < 0) return ERRORS.INVALID_INDEX;
            if (RXTX == "RX") { try { mappingRX.Add(name, index); } catch (ArgumentException ex) { return ERRORS.INVALID_NAME; } }
            else if (RXTX == "TX") { try { mappingTX.Add(name, index); } catch (ArgumentException ex) { return ERRORS.INVALID_NAME; } }
            else { return ERRORS.INVALID_RXTX; }
            return ERRORS.NO_ERROR;
        }

        public int[] GetIndex(string[] name, string RXTX) {
            int[] returnValue = new int[name.Length];
            if (name.Length == 0) { return new int[] { ERRORS.INVALID_NAME }; }
            else
            {
                if (RXTX == "RX")
                {
                    try
                    {
                        for (int cpt = 0; cpt < name.Length; cpt++) { returnValue[cpt] = mappingRX[name[cpt]]; }
                        return returnValue;
                    }
                    catch (Exception ex) { return (new int[] { ERRORS.INVALID_NAME }); }
                }
                else if (RXTX == "TX")
                {
                    try
                    {
                        for (int cpt = 0; cpt < name.Length; cpt++) { returnValue[cpt] = mappingTX[name[cpt]];  }
                        return returnValue;
                    }
                    catch (Exception ex) { return (new int[] { ERRORS.INVALID_NAME }); }
                }
                else { return (new int[] { ERRORS.INVALID_RXTX }); }
            }
        }

        public int InitFSFB2DataFlow(string nameNode, FSFB2Node myNode) {
            // Definition of the different constants used for RegEx the hcf/ccf files
            const string REMHOSTIP = @"^(RemHost = )(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})(:\d{1,5})(,\s+)(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})(:\d{1,5})$";
            const string SUBNETregexp = @"^(Subnet = -)(\d+)$";
            const string ADDRESSregexp = @"^(Address = )(\d+)$";
            const string REMADDRESSregexp = @"^(RxComAppl = )(\d+)$";
            const string BUF_TX_APPregexp = @"^(TxBuffer = )(\w+)$";
            const string BUF_RX_APPregexp = @"^(RxBuffer = )(\w+)$";
            const string NUMELEMENTSregexp = @"^(NumElements = )(\d+)$";
            const string BUF_TXregexp = @"(^Elements = |^)(\w+|\#)(,?)$";
            const string BUF_RXregexp = @"(^Elements = |^)(\w+|\#)(:\s\d+\.\d+)(,?)$";

            string hostSection = @"^\[(\w)*(" + nameNode + @")(\w)*\]$";
            string hcfFile = myNode.NameHost + ".hcf";
            string ccfFile = myNode.NameHost + ".ccf";
            string TXBufferName, RXBufferName; // Local variables for parsing the bitmap
            TXBufferName = RXBufferName = null;

            Boolean inHostSection, inTxSection, inRxSection;
            Boolean SUBNETcompleted, ADDRESScompleted, REMADDRESScompleted, TXBufferNamecompleted, RXBufferNamecompleted;
            inHostSection = inTxSection = inRxSection = false;
            SUBNETcompleted = ADDRESScompleted = REMADDRESScompleted = TXBufferNamecompleted = RXBufferNamecompleted = false;

            string[] lines; // array of string used to read the input file
            int returnCode = ERRORS.NO_ERROR;
            int numElements = 0; // variable used to track the number of elements read
            int cpt = 0; // counter for tracking the current variable rank (vs. numElements)

            // Parse the ccf file
            try {
                lines = System.IO.File.ReadAllLines(@ccfFile);
            }
            catch (System.IO.IOException exp) {
                return(ERRORS.INVALID_CCF);
            }
            foreach (string line in lines) {
                if ( Regex.IsMatch(line, hostSection) ) { inHostSection = true; }
                if (inHostSection && Regex.IsMatch(line, REMHOSTIP)) {
                    string[] IPaddress = Regex.Split(line, REMHOSTIP);
                    IPAddressRed = (IPaddress[2] + "." + IPaddress[3] + "." + IPaddress[4] + "." + IPaddress[5]);
                    IPAddressBlue = (IPaddress[8] + "." + IPaddress[9] + "." + IPaddress[10] + "." + IPaddress[11]);
                    NameNode = nameNode;
                    inHostSection = false;
                }
            }
            // Parse the hcf file
            inHostSection = false;
            try {
                lines = System.IO.File.ReadAllLines(@hcfFile);
            }
            catch (System.IO.IOException exp) {
                return (ERRORS.INVALID_HCF);
            }
            foreach (string line in lines) {
                if (Regex.IsMatch(line, hostSection)) { inHostSection = true; }
                if (inHostSection) {
                    if (Regex.IsMatch(line, BUF_TX_APPregexp)) {
                        String[] splitString = Regex.Split(line, BUF_TX_APPregexp);
                        TXBufferName = "[" + splitString[2] + "]";
                        TXBufferNamecompleted = true;
                    }
                    if (Regex.IsMatch(line, BUF_RX_APPregexp)) {
                        String[] splitString = Regex.Split(line, BUF_RX_APPregexp);
                        RXBufferName = "[" + splitString[2] + "]";
                        RXBufferNamecompleted = true;
                    }
                    if (Regex.IsMatch(line, SUBNETregexp)) {
                        String[] splitString = Regex.Split(line, SUBNETregexp);
                        Subnet = Convert.ToInt32(splitString[2]);
                        SUBNETcompleted = true;
                    }
                    if (Regex.IsMatch(line, ADDRESSregexp)) {
                        String[] splitString = Regex.Split(line, ADDRESSregexp);
                        SCAddress = Convert.ToInt32(splitString[2]);
                        ADDRESScompleted = true;
                    }
                    if (Regex.IsMatch(line, REMADDRESSregexp)) {
                        String[] splitString = Regex.Split(line, REMADDRESSregexp);
                        SIOAddress = Convert.ToInt32(splitString[2]);
                        REMADDRESScompleted = true;
                    }
                    if (SUBNETcompleted && ADDRESScompleted && REMADDRESScompleted && TXBufferNamecompleted && RXBufferNamecompleted) 
                        { inHostSection = false; }
                }
                if (line == TXBufferName) { inTxSection = true; cpt = 0; }
                if (line == RXBufferName) { inRxSection = true; cpt = 0; }
                if (inTxSection) {
                    if (Regex.IsMatch(line, NUMELEMENTSregexp)) {
                        String[] splitString = Regex.Split(line, NUMELEMENTSregexp);
                        numElements = Convert.ToInt32(splitString[2]);
                    }
                    if (Regex.IsMatch(line, BUF_TXregexp)) {
                        string[] splitLine = Regex.Split(line, BUF_TXregexp);
                        if (splitLine[2] != "#") { SetNameIndex(splitLine[2], cpt++, "TX"); }
                        else { SetNameIndex("#" + Convert.ToString(cpt), cpt++, "TX"); }
                        if (cpt == numElements) { inTxSection = false; }
                    }
                }
                if (inRxSection) {
                    if (Regex.IsMatch(line, NUMELEMENTSregexp)) {
                        String[] splitString = Regex.Split(line, NUMELEMENTSregexp);
                        numElements = Convert.ToInt32(splitString[2]);
                    }
                    if (Regex.IsMatch(line, BUF_RXregexp)) {
                        string[] splitLine = Regex.Split(line, BUF_RXregexp);
                        SetNameIndex(splitLine[2], cpt++, "RX");
                        if (cpt == numElements) { inRxSection = false; }
                    }
                }
            }
            return returnCode;
        }
    }
}
