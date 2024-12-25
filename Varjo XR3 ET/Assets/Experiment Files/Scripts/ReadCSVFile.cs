using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class ReadCSVFile
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void ReadCSVFileSimple(string path, string fileName)
    {
        StreamReader strReader = new StreamReader(path + fileName);
        bool endOfFile = false;
        while(!endOfFile)
        {
            string data_String = strReader.ReadLine();
            if(data_String == null)
            {
                endOfFile = true;
                break;
            }
            var data_values = data_String.Split(',');
            //for (int o = 0; o < data_values.Length; o++)
            //{
            //    Debug.Log("Value:" + o.ToString() + " " + data_values[o].ToString())
            //}

            Debug.Log(data_values[0].ToString() + " " + data_values[1].ToString() + " " + data_values[2].ToString() + " " + data_values[3].ToString());
        }
        
    }

    public (List<double>values_columnA, List<double> values_columnB, List<double> values_columnC, List<double> values_columnD) ReadCSVFileToColumns(string path, string fileName)
    {
        StreamReader strReader = new StreamReader(path + fileName);
        bool endOfFile = false;
        List<double> values_columnA = new List<double>();
        List<double> values_columnB = new List<double>();
        List<double> values_columnC = new List<double>();
        List<double> values_columnD = new List<double>();

        while(!endOfFile)
        {
            string data_String = strReader.ReadLine();

            if(data_String == null)
            {
                endOfFile = true;
                break;
            }

            var data_values = data_String.Split(',');
            values_columnA.Add(Convert.ToDouble(data_values[0]));
            values_columnB.Add(Convert.ToDouble(data_values[1]));
            values_columnC.Add(Convert.ToDouble(data_values[2]));
            values_columnD.Add(Convert.ToDouble(data_values[3]));
        }
        
        return (values_columnA, values_columnB, values_columnC, values_columnD);
    }


    public Tuple<List<float>, List<float>, List<float>> ReadCSVFileToColumnsABC(string path, string fileName)
    {
        StreamReader strReader = new StreamReader(path + fileName);
        bool endOfFile = false;
        List<float> values_columnA = new List<float>();
        List<float> values_columnB = new List<float>();
        List<float> values_columnC = new List<float>();
        
        // read header line
        string data_String = strReader.ReadLine();

        while(!endOfFile)
        {
            // read data lines
            data_String = strReader.ReadLine();

            if(data_String == null)
            {
                endOfFile = true;
                break;
            }

            
            var data_values = data_String.Split(',');
            
            values_columnA.Add(Convert.ToSingle(data_values[0]));
            values_columnB.Add(Convert.ToSingle(data_values[1]));
            values_columnC.Add(Convert.ToSingle(data_values[2]));
        }
        
        return new Tuple<List<float>, List<float>, List<float>>(values_columnA, values_columnB, values_columnC);
    }
}