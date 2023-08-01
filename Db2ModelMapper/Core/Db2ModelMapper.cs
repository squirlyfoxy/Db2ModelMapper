using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Db2ModelMapper.Core.ModelsAttributes;
using Db2ModelMapper.Core.Utils;

namespace Db2ModelMapper.Core
{
    public class Db2ModelMapper<T>
        where T : class
    {
        /// <summary>
        /// Creates a basic model mapper for <typeparamref name="T"/>.
        /// Automatically reads the configuration file and connects to the db.
        /// </summary>
        public Db2ModelMapper()
        {
            this.Configuration = Configurator.GetConfiguration();
        }

        public string FileName
        {
            get
            {
                var attrs = (typeof(T)).GetCustomAttributes(true);

                foreach (var attr in attrs)
                {
                    if (attr is Db2File)
                    {
                        return ((Db2File)attr).FileName;
                    }
                }

                throw new ArgumentException("T is not mapped as a db2 file");
            }
        }

        /// <summary>
        /// The givven configuration
        /// </summary>
        public Configurator Configuration { get; private set; }

        /// <summary>
        /// Get select result as a Dictionary accordin to the Db2KeyValue attribute.
        /// </summary>
        /// <param name="searchSelector">where clause</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Dictionary<string, string> KeyValueSelect(List<Db2KeyValue<T>> searchSelector)
        {
            if (!typeof(T).GetProperties().Any(p => p.GetCustomAttribute<Db2KeyValue>() != null))
            {
                throw new ArgumentException("T is not a KeyValue table");
            }

            var keyProperty = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<Db2KeyValue>() != null
                                                            && (p.GetCustomAttribute<Db2KeyValue>() as Db2KeyValue).Usage == Db2KeyValueUsage.Key);

            var valueProperty = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<Db2KeyValue>() != null
                                                            && (p.GetCustomAttribute<Db2KeyValue>() as Db2KeyValue).Usage == Db2KeyValueUsage.Value);

            var select = this.Select(searchSelector);

            var toReturn = new Dictionary<string, string>();

            foreach (var x in select)
            {
                toReturn.Add(keyProperty.GetValue(x) as string, valueProperty.GetValue(x) as string);
            }

            return toReturn;
        }

        /// <summary>
        /// Select by a where clause
        /// </summary>
        /// <param name="searchSelector">where clause</param>
        /// <returns></returns>
        public List<T> Select(List<Db2KeyValue<T>> searchSelector)
        {
            var query = this.BuildSelectQuery(searchSelector);

            var toReturn = new List<T>();

            try
            {
                using (var session = this.Connect())
                {
                    var sqlCommand = session.CreateCommand();
                    sqlCommand.CommandText = query;

                    // Execute query
                    var result = sqlCommand.ExecuteReader();

                    while (result.Read())
                    {
                        var ixQ = Activator.CreateInstance(typeof(T));

                        // Get Propery name
                        for (var c = 0; c < result.FieldCount; c++)
                        {
                            Db2Data column = null;
                            var propName = string.Empty;

                            foreach (var prop in typeof(T).GetProperties())
                            {
                                column = prop.GetCustomAttribute<Db2Data>();
                                if (column != null && column.Column == result.GetName(c))
                                {
                                    propName = prop.Name;
                                    break;
                                } else
                                {
                                    column = null;
                                }
                            }

                            if (column != null)
                            {
                                if (!TrySetProperty(ixQ, column, propName, result[c]))
                                {
                                    throw new Exception("Errore impostazione valore proprietà");
                                }
                            }
                            else
                            {
                                Logger.Warn($"[ModelMapper/Select] -> Property not found for value '{result[c].ToString().TrimEnd()}' at index {c}. Skipped");
                            }
                        }

                        toReturn.Add(ixQ as T);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("[ModelMapper/Select] -> Error while query execution", ex);
            }

            return toReturn;
        }

        /// <summary>
        /// Update given <typeparamref name="T"/> entity.
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <returns></returns>
        public bool Update(T entity)
        {
            try
            {
                var keyValueDic = new Dictionary<string, string>();
                var keyValuePrimaryKeys = new Dictionary<string, string>();
                foreach (var prop in (typeof(T)).GetProperties())
                {
                    var db2data = prop.GetCustomAttribute<Db2Data>();
                    if (db2data != null)
                    {
                        var value = prop.GetValue(entity);
                        if (prop.PropertyType.BaseType == typeof(Enum))
                        {
                            value = (prop.GetValue(entity).ToString())[0];
                        }
                        else if (prop.PropertyType == typeof(DateTime))
                        {
                            // Gestione delle date, default format: yyyy-MM-dd
                            var format = db2data.CustomFormat ?? "yyyy-MM-dd";
                            value = ((DateTime)prop.GetValue(entity)).ToString(format);
                        }

                        if (prop.GetCustomAttribute<Db2Key>() != null)
                        {
                            // where clause
                            keyValuePrimaryKeys.Add(db2data.Column, QueryUtility.AbjustValue(value.ToString()));
                        }
                        else
                        {
                            keyValueDic.Add(db2data.Column, QueryUtility.AbjustValue(value.ToString()));
                        }
                    }
                }

                Logger.Debug($"{keyValueDic.Count} columns ready to update");

                var query = this.BuildUpdateQuery(keyValueDic, keyValuePrimaryKeys);

                using (var session = this.Connect())
                {
                    var sqlCommand = session.CreateCommand();
                    sqlCommand.CommandText = query;

                    var result = sqlCommand.ExecuteNonQuery();
                    if (result == 0)
                    {
                        Logger.Warn("Query execution effected no rows");
                    }
                    else
                    {
                        Logger.Info($"{result} rows effected");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[ModelMapper/Update] -> Error while query execution/preparation for entity of type {typeof(T).Name}", ex);
                return false;
            }

            return true;
        }

        #region Metodi di utilità

        private string BuildUpdateQuery(Dictionary<string, string> keyValuePairs, Dictionary<string, string> primaryKeys)
        {
            var query = "UPDATE ";
            var i = 0;

            if (string.IsNullOrEmpty(Configuration.Library))
            {
                query += FileName;
            }
            else
            {
                query += Configuration.Library + "." + FileName;
            }

            #region Build Set

            query += " SET ";

            foreach (var column in keyValuePairs)
            {
                query += column.Key + " = '" + column.Value + "'";

                if (i < keyValuePairs.Count - 1)
                {
                    query += ", ";
                }

                i++;
            }

            #endregion

            #region Build Where

            query += " WHERE ";

            i = 0;
            foreach (var pr in primaryKeys)
            {
                query += "TRIM(" + pr.Key + ") = '" + pr.Value + "'";

                if (i < primaryKeys.Count - 1)
                {
                    query += " AND ";
                }

                i++;
            }

            #endregion

            this.LogQuery(query);

            return query;
        }

        private string BuildSelectQuery(List<Db2KeyValue<T>> searchSelector)
        {
            var query = "SELECT * ";

            #region Build from

            if (string.IsNullOrEmpty(Configuration.Library))
            {
                query += " FROM " + FileName;
            }
            else
            {
                query += " FROM " + Configuration.Library + "." + FileName;
            }

            #endregion

            #region Build where

            if (searchSelector.Any(s => s.Filter))
            {
                query += " WHERE ";
                var i = 0;
                foreach (var search in searchSelector.Where(s => s.Filter))
                {
                    // TODO: GESTIONE ALTRI COMPARATORI
                    query += search.PropertyToSearch + " = '" + search.ValueToSearch + "'";

                    if (i < searchSelector.Count - 1)
                    {
                        // TODO: GESTIONE ALTRI TIPI DI FILTRI
                        query += " AND ";
                    }

                    i++;
                }
            }

            #endregion

            this.LogQuery(query);

            return query;
        }

        private OdbcConnection Connect()
        {
            var conn = new OdbcConnection(Configuration.ConnectionString);

            conn.Open();

            return conn;
        }

        // https://stackoverflow.com/questions/16962727/how-to-set-properties-on-a-generic-entity
        private bool TrySetProperty(object obj, Db2Data property, string propertyName, object value)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    if (prop.PropertyType.BaseType == typeof(Enum))
                    {
                        value = Enum.ToObject(prop.PropertyType, (value.ToString())[0]);
                    }
                    else if (prop.PropertyType == typeof(DateTime))
                    {
                        // Gestione delle date, default format: yyyy-MM-dd
                        var format = property.CustomFormat ?? "yyyy-MM-dd";
                        var success = DateTime.TryParseExact(value.ToString(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tmp);

                        if (success)
                        {
                            value = tmp;
                        }
                        else
                        {
                            Logger.Warn($"Unable to cast value {value} to DateTime with specified format, value will be DateTime.Min");

                            value = DateTime.MinValue;
                        }
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        // Pulisce caratteri in eccesso dovuto alla lunghezza fissa
                        value = value.ToString().Trim();
                    }

                    prop.SetValue(obj, value, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Errore in TrySetProperty", ex);
            }

            return false;
        }

        private void LogQuery(string query)
        {
            if (Configuration.TraceQuery)
            {
                Logger.Info($"[ModelMapper/Query, {FileName}] ->\n {query}");
            }
        }

        #endregion
    }
}
