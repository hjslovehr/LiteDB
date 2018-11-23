﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Create a new index (or do nothing if already exists) to a collection/field
        /// </summary>
        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (expression.IsIndexable == false) throw new ArgumentException("Index expressions must contains at least one document field. Used methods must be immutable. Parameters are not supported.", nameof(expression));

            //**if (name.Length > INDEX_NAME_MAX_LENGTH) throw LiteException.InvalidIndexName(name, collection, "MaxLength = " + INDEX_PER_COLLECTION);
            if (!name.IsWord()) throw LiteException.InvalidIndexName(name, collection, "Use only [a-Z$_]");
            if (name.StartsWith("$")) throw LiteException.InvalidIndexName(name, collection, "Index name can't starts with `$`");

            if (name == "_id") return false; // always exists

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, true);
                var col = snapshot.CollectionPage;
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);

                // check if index already exists
                var current = col.GetIndex(name);

                // if already exists, just exit
                if (current != null)
                {
                    // but if expression are different, throw error
                    if (current.Expression != expression.Source) throw LiteException.IndexAlreadyExist(name);

                    return false;
                }

                // create index head
                var index = indexer.CreateIndex(name, expression.Source, unique);

                index.Name = name;
                index.Expression = expression.Source;
                index.Unique = unique;

                /*
                // read all objects (read from PK index)
                foreach (var pkNode in new IndexAll("_id", LiteDB.Query.Ascending).Run(col, indexer))
                {
                    transaction.Safepoint();

                    // read binary and deserialize document (select only used field)
                    var buffer = data.Read(data.GetBlock(pkNode.DataBlock));
                    var doc = _bsonReader.Deserialize(buffer, expression.Fields).AsDocument;

                    // get values from expression in document
                    var keys = expression.Execute(doc, true);

                    // adding index node for each value
                    foreach (var key in keys)
                    {
                        // insert new index node
                        var node = indexer.AddNode(index, key, pkNode);

                        // link index node to datablock
                        node.DataBlock = pkNode.DataBlock;
                    }
                }*/

                return true;
            });
        }

        /*
        /// <summary>
        /// Drop an index from a collection
        /// </summary>
        public bool DropIndex(string collection, string name)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            if (name == "_id") throw LiteException.IndexDropId();

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, false);
                var col = snapshot.CollectionPage;
                var indexer = new IndexService(snapshot);
            
                // no collection, no index
                if (col == null) return false;
            
                // search for index reference
                var index = col.GetIndex(name);
            
                // no index, no drop
                if (index == null) return false;

                // delete all data pages + indexes pages
                indexer.DropIndex(index);
            
                // clear index reference
                index.Clear();
            
                // mark collection page as dirty
                snapshot.SetDirty(col);
            
                return true;
            });
        }
        */
    }
}