﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Orchard.Environment.Configuration;
using Orchard.FileSystems.AppData;
using Orchard.Indexing;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;
using Orchard.Logging;
using System.Xml.Linq;

namespace Orchard.Core.Indexing.Lucene {
    /// <summary>
    /// Represents the default implementation of an IIndexProvider, based on Lucene
    /// </summary>
    public class DefaultIndexProvider : IIndexProvider {
        private readonly IAppDataFolder _appDataFolder;
        private readonly ShellSettings _shellSettings;
        public static readonly Version LuceneVersion = Version.LUCENE_29;
        private readonly Analyzer _analyzer = new StandardAnalyzer(LuceneVersion);
        private readonly string _basePath;
        public static readonly DateTime DefaultMinDateTime = new DateTime(1980, 1, 1);
        public static readonly string Settings = "Settings";
        public static readonly string LastIndexUtc = "LastIndexedUtc";

        public ILogger Logger { get; set; }

        public DefaultIndexProvider(IAppDataFolder appDataFolder, ShellSettings shellSettings) {
            _appDataFolder = appDataFolder;
            _shellSettings = shellSettings;

            // TODO: (sebros) Find a common way to get where tenant's specific files should go. "Sites/Tenant" is hard coded in multiple places
            _basePath = Path.Combine("Sites", _shellSettings.Name, "Indexes");

            Logger = NullLogger.Instance;

            // Ensures the directory exists
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists() {
            var directory = new DirectoryInfo(_appDataFolder.MapPath(_basePath));
            if(!directory.Exists) {
                directory.Create();
            }
        }

        protected virtual Directory GetDirectory(string indexName) {
            var directoryInfo = new DirectoryInfo(_appDataFolder.MapPath(Path.Combine(_basePath, indexName)));
            return FSDirectory.Open(directoryInfo);
        }

        private static Document CreateDocument(DefaultIndexDocument indexDocument) {
            var doc = new Document();

            indexDocument.PrepareForIndexing();
            foreach(var field in indexDocument.Fields) {
                doc.Add(field);
            }
            return doc;
        }

        public bool Exists(string indexName) {
            return new DirectoryInfo(_appDataFolder.MapPath(Path.Combine(_basePath, indexName))).Exists;
        }

        public bool IsEmpty(string indexName) {
            if ( !Exists(indexName) ) {
                return true;
            }

            var reader = IndexReader.Open(GetDirectory(indexName), true);

            try {
                return reader.NumDocs() == 0;
            }
            finally {
                reader.Close();
            }
        }

        public int NumDocs(string indexName) {
            if ( !Exists(indexName) ) {
                return 0;
            }

            var reader = IndexReader.Open(GetDirectory(indexName), true);

            try {
                return reader.NumDocs();
            }
            finally {
                reader.Close();
            }
        }

        public void CreateIndex(string indexName) {
            var writer = new IndexWriter(GetDirectory(indexName), _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            writer.Close();

            Logger.Information("Index [{0}] created", indexName);
        }

        public void DeleteIndex(string indexName) {
            new DirectoryInfo(Path.Combine(_appDataFolder.MapPath(Path.Combine(_basePath, indexName))))
                .Delete(true);

            var settingsFileName = GetSettingsFileName(indexName);
            if(File.Exists(settingsFileName)) {
                File.Delete(settingsFileName);
            }
        }

        public void Store(string indexName, IIndexDocument indexDocument) {
            Store(indexName, new [] { (DefaultIndexDocument)indexDocument });
        }

        public void Store(string indexName, IEnumerable<IIndexDocument> indexDocuments) {
            Store(indexName, indexDocuments.Cast<DefaultIndexDocument>());
        }

        public void Store(string indexName, IEnumerable<DefaultIndexDocument> indexDocuments) {
            if(indexDocuments.AsQueryable().Count() == 0) {
                return;
            }

            var writer = new IndexWriter(GetDirectory(indexName), _analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
            DefaultIndexDocument current = null;

            try {
                foreach ( var indexDocument in indexDocuments ) {
                    current = indexDocument;
                    var doc = CreateDocument(indexDocument);
                    writer.AddDocument(doc);
                    Logger.Debug("Document [{0}] indexed", indexDocument.Id);
                }
            }
            catch ( Exception ex ) {
                Logger.Error(ex, "An unexpected error occured while add the document [{0}] from the index [{1}].", current.Id, indexName);
            }
            finally {
                writer.Optimize();
                writer.Close();
            }
        }

        public void Delete(string indexName, int documentId) {
            Delete(indexName, new[] { documentId });
        }

        public void Delete(string indexName, IEnumerable<int> documentIds) {
            if ( documentIds.AsQueryable().Count() == 0 ) {
                return;
            }
            
            var reader = IndexReader.Open(GetDirectory(indexName), false);

            try {
                foreach (var id in documentIds) {
                    try {
                        var term = new Term("id", id.ToString());
                        if (reader.DeleteDocuments(term) != 0) {
                            Logger.Error("The document [{0}] could not be removed from the index [{1}]", id, indexName);
                        }
                        else {
                            Logger.Debug("Document [{0}] removed from index", id);
                        }
                    }
                    catch (Exception ex) {
                        Logger.Error(ex, "An unexpected error occured while removing the document [{0}] from the index [{1}].", id, indexName);
                    }
                }
            }
            finally {
                reader.Close();
            }
        }

        public IIndexDocument New(int documentId) {
            return new DefaultIndexDocument(documentId);
        }

        public ISearchBuilder CreateSearchBuilder(string indexName) {
            return new DefaultSearchBuilder(GetDirectory(indexName));
        }

        private string GetSettingsFileName(string indexName) {
            return Path.Combine(_appDataFolder.MapPath(_basePath), indexName + ".settings.xml");
        }

        public DateTime GetLastIndexUtc(string indexName) {
            var settingsFileName = GetSettingsFileName(indexName);

            return File.Exists(settingsFileName) 
                ? DateTime.Parse(XDocument.Load(settingsFileName).Descendants(LastIndexUtc).First().Value)
                : DefaultMinDateTime;
        }

        public void SetLastIndexUtc(string indexName, DateTime lastIndexUtc) {
            if ( lastIndexUtc < DefaultMinDateTime ) {
                lastIndexUtc = DefaultMinDateTime;
            }

            XDocument doc;
            var settingsFileName = GetSettingsFileName(indexName);
            if ( !File.Exists(settingsFileName) ) {
                EnsureDirectoryExists();
                doc = new XDocument(
                        new XElement(Settings,
                            new XElement(LastIndexUtc, lastIndexUtc.ToString("s"))));
            }
            else {
                doc = XDocument.Load(settingsFileName);
                doc.Element(Settings).Element(LastIndexUtc).Value = lastIndexUtc.ToString("s");
            }

            doc.Save(settingsFileName);
        }

    }
}
