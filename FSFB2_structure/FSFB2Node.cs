using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace FSFB2_structure {
    public class FSFB2Node {
        // This class contains all the useful info for a node on the FSFB2 network
        public string NameHost { get; set; }
        private string[] ListNodes;
        public string[] getListNodes() { return ListNodes; }

        public int InitListNotes() {
            /// This method creates the list of nodes documented for the input files identified under 'NameHost'
            const string listNodes = @"^(fsfb2 = )(((,\s+)?(APP_)(\D{3}\d?))+)(,\s+)?$";
            const string listNodesMultiLine = @"^(((,?)(\s+)?(APP_)(\D{3}\d?))+)(,\s)?$";
            bool multiLine = false;
            string hcfFile = NameHost + ".hcf";
            string ccfFile = NameHost + ".ccf";
            string[] lines;

            try {
                lines = System.IO.File.ReadAllLines(@hcfFile);
            }
            catch (System.IO.IOException exp) {
                return ERRORS.INVALID_HCF;
            }
            foreach (string line in lines) {
                if ( !multiLine && (Regex.IsMatch(line, listNodes)) ) {
                    String[] splitString = Regex.Split(line, listNodes);
                    char[] separator = { ',' , ' '};
                    ListNodes = splitString[2].Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    int cpt = 0;
                    foreach(string node in ListNodes) {
                        String[] spltString = Regex.Split(node, @"^(APP_)(\D{3}\d?)$");
                        ListNodes[cpt++] = spltString[2];
                    }
                    multiLine = true;
                }
                else if (multiLine && (Regex.IsMatch(line, listNodesMultiLine)) ) {
                    String[] splitString = Regex.Split(line, listNodes);
                    char[] separator = { ',', ' ' };
                    string[] ListNodes_add = splitString[0].Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    int cpt = ListNodes.Length;
                    Array.Resize(ref ListNodes, ListNodes.Length + ListNodes_add.Length);
                    foreach (string node in ListNodes_add) {
                        String[] spltString = Regex.Split(node, @"^(APP_)(\D{3}\d?)$");
                        ListNodes[cpt++] = spltString[2];
                    }
                }
                else if (multiLine == true) { multiLine = false; }
            }
            return ERRORS.NO_ERROR;
        }

    }
}
