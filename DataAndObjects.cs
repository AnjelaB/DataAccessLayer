using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;
using System.IO;
using System.Reflection;

namespace DataAccessLayer
{
    /// <summary>
    /// Interface for Data Access Layer.
    /// </summary>
    /// <typeparam name="TEntity">Type of objecs that will be created.</typeparam>
    interface DataAccessLayer<TEntity>
    {
        IEnumerable<TEntity> GetData(string code, ICollection<KeyValuePair<string, object>> parametres);
        void CreateConnection(string dataBase);
    }

    /// <summary>
    /// Class that implement Data Access Layer interface.
    /// </summary>
    /// <typeparam name="TEntity">Type of objecs that will be created.</typeparam>
    public class DataAndObjects<TEntity> : DataAccessLayer<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// Field for connecting sql server.
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// Sql Server Connection creating.
        /// </summary>
        /// <param name="dataBase">Database that will be used.</param>
        public void CreateConnection(string dataBase)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.InitialCatalog = dataBase;
            builder.IntegratedSecurity = true;
            builder.DataSource = "(local)";
            var connectionString = builder.ConnectionString;
            this.connection = new SqlConnection(connectionString);

        }

        /// <summary>
        /// Method that find necessary Query or Procedure From the file.
        /// </summary>
        /// <param name="code">Function name.</param>
        /// <param name="parametrs">Parametres that are used.</param>
        /// <param name="command">Command that is used in GetData method.</param>
        private string FindQuery(string code, ICollection<KeyValuePair<string, object>> parametrs,ref DbCommand command)
        {
            string query = " ";
            List<KeyValuePair<string, object>> parametrs1 = new List<KeyValuePair<string, object>>();
            var file = File.ReadLines(@"C:\Users\User\Desktop\SqlQueriesAndProcedure.txt", Encoding.UTF8).GetEnumerator();
            while(file.MoveNext())
            {
                
                if(file.Current.Contains("Function Name") && file.Current.Contains(code))
                {
                    file.MoveNext();
                    while (file.Current != "_End")
                    {
                        
                        if (file.Current.Contains("Parametres"))
                        {
                            var elements = file.Current.Split(':', ',', '-');
                            foreach(var parametr in parametrs)
                            {
                                for (int i = 0; i < elements.Length; i++)
                                {
                                    if (elements[i] == parametr.Key)
                                    {
                                        command.Parameters.Add(new SqlParameter(elements[i - 1].Trim(), parametr.Key));
                                    }
                                }
                            }
                        }
                        else if(file.Current.Contains("Function Type"))
                        {
                            var type = file.Current.Split(':');
                            if (type[1].Trim() == "Text")
                            {
                                command.CommandType = CommandType.Text;
                            }
                            else
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                file.MoveNext();
                                var procedure = file.Current.Split(' ', '(', ')');
                                query += procedure[2];
                                break;
                            }
                            
                        }
                        else
                        {
                            query += file.Current + " ";
                        }
                        file.MoveNext();
                    }
                    break;
                }
            }
            return query;
        }

        /// <summary>
        /// Methad that get function name and parameters and returns result from executing.
        /// </summary>
        /// <param name="code">Function name.</param>
        /// <param name="parametres">Parametres.</param>
        public IEnumerable<TEntity> GetData(string code, ICollection<KeyValuePair<string, object>> parametres)
        {

            using (this.connection)
            {

                DbCommand command = this.connection.CreateCommand();
                command.CommandText = FindQuery(code,parametres,ref command);
                command.Connection = this.connection;
                this.connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    IEnumerable<TEntity> rows = GetEntityObjects(reader);
                    return rows;
                }
            }
        }

        /// <summary>
        /// Methad that get TEntity type objects from data.
        /// </summary>
        /// <param name="reader">Result from executing code.</param>
        private IEnumerable<TEntity> GetEntityObjects(DbDataReader reader)
        {
            var propertiesInfo = typeof(TEntity).GetProperties();
            List<TEntity> list = new List<TEntity>();
            while(reader.Read())
            {
                var entity = new TEntity();
                for (int index = 0; index < reader.FieldCount; index++)
                {
                    
                    foreach (var property in propertiesInfo)
                    {
                        var el = reader.GetName(index);
                        if (property.Name == reader.GetName(index))
                        {

                            var propertyValue = reader.GetValue(index);
                            if (propertyValue != DBNull.Value)
                            {
                                ParsePrimitive(property, entity, propertyValue);
                                break;
                            }
                        }
                    }
                    
                }
                list.Add(entity);
            }
            return list;
        }

        /// <summary>
        /// Methad that set value to property.
        /// </summary>
        /// <param name="prop">Entity's property info.</param>
        /// <param name="entity">Entity object</param>
        /// <param name="value">Valu of the property.</param>
        private static void ParsePrimitive(PropertyInfo prop, object entity, object value)
        {
            if (prop.PropertyType == typeof(string))
            {
                prop.SetValue(entity, value.ToString().Trim(), null);
            }
            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
            {
                if (value == null)
                {
                    prop.SetValue(entity, null, null);
                }
                else
                {
                    prop.SetValue(entity, int.Parse(value.ToString()), null);
                }
            }
            else if(prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
            {
                if (value == null)
                {
                    prop.SetValue(entity, null, null);
                }
                else
                {
                    prop.SetValue(entity, double.Parse(value.ToString()), null);
                }
            }
        }

    }


}

