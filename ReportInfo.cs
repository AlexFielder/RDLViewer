// This sample code is courtesy of http://www.gotreportviewer.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;

namespace RdlViewer
{
    /// <summary>
    /// Think of this class as a "Reflector for RDL". It digs into the XML refinition of the RDL and
    /// extracts useful information.
    /// </summary>
    class ReportInfo
    {
        private string m_rdlFilename;
        private Dictionary<string, DataSourceInfo> m_dataSourceDictionary;
        private Dictionary<string, DataSetInfo> m_dataSetDictionary;

        public ReportInfo(string rdlFilename)
        {
            this.m_rdlFilename = rdlFilename;
        }

        private void ExtractReferencedDataSourceInfo(string dataSourceName, string dataSourceReference)
        {
            XmlDocument rdsDocument = new XmlDocument();
            string rdsFilename = Path.GetDirectoryName(m_rdlFilename) + "\\" + dataSourceReference + ".rds";
            rdsDocument.Load(rdsFilename);

            XmlNode connectionPropsNode = rdsDocument.SelectSingleNode("RptDataSource/ConnectionProperties");
            XmlNode connectStringNode = connectionPropsNode.SelectSingleNode("ConnectString");
            string connectString = connectStringNode.InnerText;
            m_dataSourceDictionary.Add(dataSourceName, new DataSourceInfo(connectString));
        }

        private void ExtractDataSourceInfo(XmlDocument rdlDocument, XmlNamespaceManager nsmgr)
        {
            m_dataSourceDictionary = new Dictionary<string, DataSourceInfo>();

            XmlNode dataSourcesNode = rdlDocument.SelectSingleNode("r:Report/r:DataSources", nsmgr);
            if (dataSourcesNode == null)
                return;
            XmlNodeList dataSourceList = dataSourcesNode.SelectNodes("r:DataSource", nsmgr);
            foreach (XmlNode dataSourceNode in dataSourceList)
            {
                string dataSourceName = dataSourceNode.Attributes.GetNamedItem("Name").InnerText;
                XmlNode connectionPropsNode = dataSourceNode.SelectSingleNode("r:ConnectionProperties", nsmgr);
                if (connectionPropsNode == null)
                {
                    XmlNode dataSourceReferenceNode = dataSourceNode.SelectSingleNode("r:DataSourceReference", nsmgr);
                    string dataSourceReference = dataSourceReferenceNode.InnerText;
                    ExtractReferencedDataSourceInfo(dataSourceName, dataSourceReference);
                }
                else
                {
                    XmlNode connectStringNode = connectionPropsNode.SelectSingleNode("r:ConnectString", nsmgr);
                    string connectString = connectStringNode.InnerText;
                    m_dataSourceDictionary.Add(dataSourceName, new DataSourceInfo(connectString));
                }
            }
        }

        private void ExtractDataSetInfo(XmlDocument rdlDocument, XmlNamespaceManager nsmgr)
        {
            m_dataSetDictionary = new Dictionary<string, DataSetInfo>();

            XmlNode dataSetsNode = rdlDocument.SelectSingleNode("r:Report/r:DataSets", nsmgr);
            if (dataSetsNode == null)
                return;
            XmlNodeList dataSetList = dataSetsNode.SelectNodes("r:DataSet", nsmgr);
            foreach (XmlNode dataSetNode in dataSetList)
            {
                string dataSetName = dataSetNode.Attributes.GetNamedItem("Name").InnerText;
                XmlNode queryNode = dataSetNode.SelectSingleNode("r:Query", nsmgr);
                XmlNode dataSourceNameNode = queryNode.SelectSingleNode("r:DataSourceName", nsmgr);
                string dataSourceName = dataSourceNameNode.InnerText;
                XmlNode commandTextNode = queryNode.SelectSingleNode("r:CommandText", nsmgr);
                string commandText = commandTextNode.InnerText;
                XmlNode parametersNode = queryNode.SelectSingleNode("r:QueryParameters", nsmgr);
                IList<QueryParameterInfo> queryParameters = null;
                if (parametersNode != null)
                {
                    queryParameters = new List<QueryParameterInfo>();
                    XmlNodeList parameterList = parametersNode.SelectNodes("r:QueryParameter", nsmgr);
                    foreach (XmlNode parameterNode in parameterList)
                    {
                        string parameterName = parameterNode.Attributes.GetNamedItem("Name").InnerText;
                        XmlNode valueNode = parameterNode.SelectSingleNode("r:Value", nsmgr);
                        string valueExpression = valueNode.InnerText;
                        QueryParameterInfo queryParameterInfo = new QueryParameterInfo(parameterName, valueExpression);
                        queryParameters.Add(queryParameterInfo);
                    }
                }

                DataSetInfo dataSetInfo = new DataSetInfo(dataSourceName, commandText);
                dataSetInfo.queryParameters = queryParameters;
                m_dataSetDictionary.Add(dataSetName, dataSetInfo);
            }
        }

        public void Initialize()
        {
            XmlDocument rdlDocument = new XmlDocument();
            rdlDocument.Load(m_rdlFilename);

            XmlElement root = rdlDocument.DocumentElement;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(rdlDocument.NameTable);
            nsmgr.AddNamespace("r", root.NamespaceURI);
            nsmgr.AddNamespace("rd", "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner");

            ExtractDataSourceInfo(rdlDocument, nsmgr);
            ExtractDataSetInfo(rdlDocument, nsmgr);
        }

        public DataTable GetData(string dataSetName, ReportParameterInfoCollection reportParams)
        {
            DataSetInfo dataSetInfo = m_dataSetDictionary[dataSetName];
            if (dataSetInfo.dataTable == null)
            {
                DataSourceInfo dataSourceInfo = m_dataSourceDictionary[dataSetInfo.dataSourceName];
                if (dataSourceInfo.connection == null)
                {
                    ConnectionDialog dialog =
                        new ConnectionDialog(dataSetInfo.dataSourceName, dataSourceInfo.connectString);
                    if (dialog.ShowDialog() == DialogResult.Cancel)
                    {
                        return null;
                    }
                    else
                    {
                        dataSourceInfo.connection = dialog.connection;
                    }
                }
                SqlCommand command = dataSourceInfo.connection.CreateCommand();
                command.CommandText = dataSetInfo.query;
                if (dataSetInfo.queryParameters != null)
                {
                    foreach (QueryParameterInfo queryParameterInfo in dataSetInfo.queryParameters)
                    {
                        string valueExpression = queryParameterInfo.valueExpression;
                        const string starting = "=Parameters!";
                        const string ending = ".Value";
                        if (!valueExpression.StartsWith(starting) ||
                            !valueExpression.EndsWith(ending))
                        {
                            throw new Exception("Can't parse query parameter expression: " + valueExpression);
                        }
                        int parameterNameLen = valueExpression.Length - starting.Length - ending.Length;
                        string parameterName = valueExpression.Substring(starting.Length, parameterNameLen);
                        string reportParameterValue = reportParams[parameterName].Values[0];

                        if (reportParameterValue == null)
                            throw new Exception("Report parameter " + parameterName + " has no value set");

                        command.Parameters.AddWithValue(queryParameterInfo.parameterName, reportParameterValue);
                    }
                }

                SqlDataReader reader = command.ExecuteReader();
                DataTable dataTable = new DataTable();
                dataTable.Load(reader);
                reader.Close();

                dataSetInfo.dataTable = dataTable;
            }
            return dataSetInfo.dataTable;
        }

        public void Cleanup()
        {
            foreach (DataSourceInfo dataSourceInfo in m_dataSourceDictionary.Values)
            {
                if (dataSourceInfo.connection != null)
                    dataSourceInfo.connection.Close();
            }
            m_dataSourceDictionary = null;
            m_dataSetDictionary = null;
        }
    }

    class DataSetInfo
    {
        public string dataSourceName;
        public string query;
        public DataTable dataTable;
        public IList<QueryParameterInfo> queryParameters;

        public DataSetInfo(string dataSourceName, string query)
        {
            this.dataSourceName = dataSourceName;
            this.query = query;
        }
    }

    class QueryParameterInfo
    {
        public string parameterName;
        public string valueExpression;

        public QueryParameterInfo(string parameterName, string valueExpression)
        {
            this.parameterName = parameterName;
            this.valueExpression = valueExpression;
        }
    }

    class DataSourceInfo
    {
        public string connectString;
        public SqlConnection connection;

        public DataSourceInfo(string connectString)
        {
            this.connectString = connectString;
        }
    }
}
