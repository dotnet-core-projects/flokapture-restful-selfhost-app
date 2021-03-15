using System.Collections.Generic;
using BusinessLayer.DbEntities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace BusinessLayer.ExtensionLibrary
{
    public static class LookupExtensions
    {
        public static IAggregateFluent<TSource> ApplyLookup<TSource>(this IAggregateFluent<TSource> lookupWrapper, Dictionary<string, EntityLookup> lookups)
        {
            var aggregateUnwindOptions = new AggregateUnwindOptions<TSource>
            {
                PreserveNullAndEmptyArrays = true
            };
            foreach (var lookup in lookups)
            {
                lookups.Remove(lookup.Key);
                return ApplyLookup(lookupWrapper
                    .Lookup(lookup.Key, lookup.Value.LocalField, lookup.Value.ForeignField, lookup.Value.As)
                    .Unwind($"{lookup.Value.As}", aggregateUnwindOptions)
                    .As<TSource>(), lookups);
            }
            return lookupWrapper;
        }

        public static IAggregateFluent<TSource> ApplyLookup<TSource>(this IAggregateFluent<TSource> fluent, List<BsonDocumentPipelineStageDefinition<TSource, TSource>> stageDefinitions)
        {
            foreach (var stageDefinition in stageDefinitions)
            {
                stageDefinitions.Remove(stageDefinition);
                return ApplyLookup(fluent.AppendStage(stageDefinition), stageDefinitions);
            }
            return fluent;
        }

        public static IAggregateFluent<TSource> AppendStages<TSource>(this IAggregateFluent<TSource> fluent, List<BsonDocument> bsonDocuments)
        {
            foreach (var bsonDocument in bsonDocuments)
            {
                bsonDocuments.Remove(bsonDocument);
                return AppendStages(fluent
                    .AppendStage(new BsonDocumentPipelineStageDefinition<TSource, TSource>(bsonDocument)), bsonDocuments);
            }
            return fluent;
        }

        public static List<BsonDocument> ApplyLookup<TSource>(this IFindFluent<TSource, TSource> findFluent, FilterDefinition<TSource> filterDefinition)
        {
            var bsonClassMap = new BsonClassMap(typeof(TSource));
            bsonClassMap.AutoMap();
            bsonClassMap.Freeze();
            var serializer = new BsonClassMapSerializer<TSource>(bsonClassMap);
            var bsonDocument = filterDefinition.Render(serializer, new BsonSerializerRegistry());
            var propertyInfo = typeof(TSource).GetProperty("Lookup");
            if (propertyInfo == null) return new List<BsonDocument>();

            var lookups = (Dictionary<string, EntityLookup>)propertyInfo.GetValue("Lookup");
            var bsonDocuments = new List<BsonDocument>();
            foreach (var lookup in lookups)
            {
                bsonDocuments.Add(new BsonDocument
                {
                    {
                        "$lookup", new BsonDocument
                        {
                            {"from", lookup.Value.From}, {"foreignField", lookup.Value.ForeignField},
                            {"localField", lookup.Value.LocalField}, {"as", lookup.Value.As}
                        }
                    }
                });

                bsonDocuments.Add(new BsonDocument
                {
                    {"$unwind", new BsonDocument {{"path", $"${lookup.Value.As}"}, {"preserveNullAndEmptyArrays", true}}}
                });
            }
            bsonDocuments.Insert(0, new BsonDocument { { "$match", bsonDocument } });
            return bsonDocuments;
        }
    }
}
