
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EE_RoofFramer.Utilities
{
    public static class FileObjects
    {
        /// <summary>
        /// Writes a line to file
        /// </summary>
        /// <param name="filename">Name of file</param>
        /// <param name="line">String of text to be written</param>
        /// <returns></returns>
        public static async Task AppendStringToFile(string filename, string line)
        {
            using (StreamWriter file = new StreamWriter(filename, true))
            {
                await file.WriteLineAsync(line);
            }
        }


        /// <summary>
        /// Reads the entire contents of a file to a string array
        /// </summary>
        /// <param name="filename">Name of the file to be read</param>
        /// <returns></returns>
        public static string[] ReadFromFile(string filename)
        {
            try
            {
                string[] lines = File.ReadAllLines(filename);
                return lines;
            } catch (IOException ioexp)
            {
                MessageBox.Show("Error reading from file [" + filename + "]:" + ioexp.ToString());
                return null;
            }

        }

        public static int DeleteFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                return 0;
            } catch (IOException ioexp)
            {
                MessageBox.Show("Error writing file [" + filename + "]:" + ioexp.ToString());
                return -1;
            }
        }
    }
}
