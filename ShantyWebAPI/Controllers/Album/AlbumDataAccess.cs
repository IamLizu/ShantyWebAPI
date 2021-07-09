﻿using Microsoft.AspNetCore.Http;
using ShantyWebAPI.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShantyWebAPI.Models.Album;
using MongoDB.Bson;
using MySql.Data.MySqlClient;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;

namespace ShantyWebAPI.Controllers.Album
{
    public class AlbumDataAccess
    {
        MysqlConnectionProvider dbConnection;
        public AlbumDataAccess()
        {
            dbConnection = new MysqlConnectionProvider();
        }
        //COMMON METHODS
        public string UploadAlbumCoverImage(IFormFile coverImage, string id)
        {
            string imageName = "albumarts/" + id;
            AzureBlobServiceProvider azureBlob = new AzureBlobServiceProvider();
            return azureBlob.UploadFileToBlob(imageName, coverImage);
        }
        public string JwtTokenValidation(string jwt)
        {
            return new JwtAuthenticationProvider().ValidateToken(jwt);
        }
        public bool IsLabel(string id)
        {
            MysqlConnectionProvider dbConnection = new MysqlConnectionProvider();
            dbConnection.CreateQuery("SELECT * FROM users WHERE id='" + id + "' AND type='label'");
            MySqlDataReader reader = dbConnection.DoQuery();
            if (reader.Read())
            {
                return true;
            }
            dbConnection.Dispose();
            dbConnection = null;
            return false;
        }

        //INSERT ALBUM
        public bool CreateAlbum(AlbumGlobalModel albumGlobalModel)
        {
            try
            {
                var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("albums");
                var document = new BsonDocument
                    {
                        { "AlbumId", albumGlobalModel.Id },
                        { "AlbumName", albumGlobalModel.AlbumName },
                        { "CoverImageUrl", albumGlobalModel.CoverImageUrl },
                        { "LabelId", albumGlobalModel.LabelId },
                        { "ArtistId", albumGlobalModel.ArtistId },
                        { "Year", albumGlobalModel.Year },
                        { "Genre", albumGlobalModel.Genre }
                    };
                collection.InsertOne(document);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        //UPDATE ALBUM
        public bool UpdateAlbum(AlbumUpdateModel albumUpdateModel)
        {
            albumUpdateModel.CoverImageUrl = UploadAlbumCoverImage(albumUpdateModel.CoverImage, albumUpdateModel.Id);
            var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("albums");
            var filter = Builders<BsonDocument>.Filter.Eq("AlbumId", albumUpdateModel.Id) & Builders<BsonDocument>.Filter.Eq("LabelId", albumUpdateModel.LabelId);
            var update = Builders<BsonDocument>.Update.Set("CoverImageUrl", albumUpdateModel.CoverImageUrl)
                .Set("AlbumName", albumUpdateModel.AlbumName)
                .Set("Year", albumUpdateModel.Year)
                .Set("ArtistId", albumUpdateModel.ArtistId)
                .Set("Genre", albumUpdateModel.Genre);
            if (collection.UpdateOne(filter, update).ModifiedCount > 0)
            {
                return true;
            }
            return false;
        }
        //GET ALBUM
        public AlbumGetModel GetAlbum(string id)
        {
            AlbumGetModel albumGetModel = null;
            var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("albums");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("AlbumId", id);
            var result = collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                albumGetModel = new AlbumGetModel();
                AlbumGetModel res = BsonSerializer.Deserialize<AlbumGetModel>(result);
                albumGetModel.AlbumId = res.AlbumId;
                albumGetModel.AlbumName = res.AlbumName;
                albumGetModel.CoverImageUrl = res.CoverImageUrl;
                albumGetModel.Year = res.Year;
                albumGetModel.Genre = res.Genre;
                albumGetModel.ArtistId = res.ArtistId;
                albumGetModel.LabelId = res.LabelId;
            }
            return albumGetModel;
        }
        //DELETE ALBUM
        public bool DeleteAlbum(string labelId, string albumId)
        {
            var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("albums");
            var deleteFilter = Builders<BsonDocument>.Filter.Eq("AlbumId", albumId) & Builders<BsonDocument>.Filter.Eq("LabelId", labelId);
            if (collection.DeleteOne(deleteFilter).DeletedCount > 0)
            {
                return true;
            }
            return false;
        }
    }
}
