﻿using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using ShantyWebAPI.Models.Album;
using ShantyWebAPI.Models.Playlist;
using ShantyWebAPI.Models.Song;
using ShantyWebAPI.Providers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ShantyWebAPI.Controllers.Playlist
{
    public class PlaylistDataAccess
    {
        //COMMON METHODS
        public string UploadPlaylistCoverImage(IFormFile coverImage, string id)
        {
            string imageName = "playlistcovers/" + id;
            AzureBlobServiceProvider azureBlob = new AzureBlobServiceProvider();
            return azureBlob.UploadFileToBlob(imageName, coverImage);
        }
        public string JwtTokenValidation(string jwt)
        {
            return new JwtAuthenticationProvider().ValidateToken(jwt);
        }
        public bool IsListenerOrArtist(string id)
        {
            MysqlConnectionProvider dbConnection = new MysqlConnectionProvider();
            dbConnection.CreateQuery("SELECT * FROM users WHERE id='" + id + "' AND type='listener' OR type='artist'");
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
        public bool CreatePlaylist(PlaylistCreateModel playlistCreateModel)
        {
            if (playlistCreateModel.PlaylistImage != null)
            {
                playlistCreateModel.PlaylistImageUrl = UploadPlaylistCoverImage(playlistCreateModel.PlaylistImage, playlistCreateModel.PlaylistId);
            }
            else
            {
                playlistCreateModel.PlaylistImageUrl = UploadPlaylistCoverImage(new DynamicPictureGenerator().GenerateNewImage(playlistCreateModel.PlaylistName[0].ToString(), 854, 854, 500), playlistCreateModel.PlaylistId);
            }
            try
            {
                var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("playlists");
                var document = new BsonDocument
                    {
                        { "PlaylistId", playlistCreateModel.PlaylistId },
                        { "PlaylistName", playlistCreateModel.PlaylistName },
                        { "PlaylistImageUrl", playlistCreateModel.PlaylistImageUrl },
                        { "CreatorId", playlistCreateModel.CreatorId },
                        { "Songs", new BsonArray() }
                    };
                collection.InsertOne(document);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //UPDATE PLAYLIST
        public bool UpdatePlaylist(PlaylistCreateModel playlistCreateModel)
        {
            if (playlistCreateModel.PlaylistImage != null)
            {
                playlistCreateModel.PlaylistImageUrl = UploadPlaylistCoverImage(playlistCreateModel.PlaylistImage, playlistCreateModel.PlaylistId);
            }
            else
            {
                playlistCreateModel.PlaylistImageUrl = UploadPlaylistCoverImage(new DynamicPictureGenerator().GenerateNewImage(playlistCreateModel.PlaylistName[0].ToString(), 854, 854, 500), playlistCreateModel.PlaylistId);
            }
            var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("playlists");
            var filter = Builders<BsonDocument>.Filter.Eq("PlaylistId", playlistCreateModel.PlaylistId);
            var update = Builders<BsonDocument>.Update.Set("PlaylistId", playlistCreateModel.PlaylistId);
            foreach (PropertyInfo prop in playlistCreateModel.GetType().GetProperties())
            {
                var value = playlistCreateModel.GetType().GetProperty(prop.Name).GetValue(playlistCreateModel, null);
                if ((prop.Name != "PlaylistId") && (prop.Name != "JwtToken") && (prop.Name != "PlaylistImage"))
                {
                    if (value != null)
                    {
                        update = update.Set(prop.Name, value);
                    }
                }
            }
            if (collection.UpdateOne(filter, update).ModifiedCount > 0)
            {
                return true;
            }
            return false;
        }

        //ADD SONG PLAYLIST
        public bool AddSongPlaylist(string userId, string playlistId, string songId)
        {
            SongGetModel songGetModel = null;
            var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("songs");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("SongId", songId);
            var result = collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                songGetModel = new SongGetModel();
                SongGetModel res = BsonSerializer.Deserialize<SongGetModel>(result);
                songGetModel.SongId = res.SongId;
                songGetModel.SongName = res.SongName;
                songGetModel.ArtistName = res.ArtistName;
                songGetModel.AlbumId = res.AlbumId;
                songGetModel.Genre = res.Genre;
                songGetModel.SongFileUrl = res.SongFileUrl;
                songGetModel.TimesStreamed = res.TimesStreamed;
                songGetModel.CoverImageUrl = res.CoverImageUrl;
            }
            else
            {
                return false;
            }
            var collectionPlaylist = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("playlists");
            var playlistFilter = Builders<BsonDocument>.Filter.Eq("CreatorId", userId) & Builders<BsonDocument>.Filter.Eq("PlaylistId", playlistId);
            var update = Builders<BsonDocument>.Update.AddToSet("Songs", new BsonDocument {
                        { "SongId", songGetModel.SongId },
                        { "SongName", songGetModel.SongName },
                        { "SongFileUrl", songGetModel.SongFileUrl },
                        { "AlbumId", songGetModel.AlbumId },
                        { "ArtistName", songGetModel.ArtistName },
                        { "TimesStreamed", songGetModel.TimesStreamed},
                        { "Genre", songGetModel.Genre },
                        { "CoverImageUrl", songGetModel.CoverImageUrl}
            });
            if(collectionPlaylist.UpdateOne(playlistFilter, update).ModifiedCount > 0)
            {
                return true;
            }
            return false;
        }

        //REMOVE SONG PLAYLIST
        public bool RemoveSongPlaylist(string userId, string playlistId, string songId)
        {
            var collectionPlaylist = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("playlists");
            var playlistFilter = Builders<BsonDocument>.Filter.Eq("CreatorId", userId) & Builders<BsonDocument>.Filter.Eq("PlaylistId", playlistId);
            var update = Builders<BsonDocument>.Update.PullFilter("Songs", Builders<BsonDocument>.Filter.Eq("SongId", songId));
            return (collectionPlaylist.UpdateOne(playlistFilter, update).ModifiedCount > 0);
        }

        //GET PLAYLIST
        public PlaylistGetModel GetPlaylist(string playlistId)
        {
            PlaylistGetModel playlistGetModel = null;
            var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("playlists");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("PlaylistId", playlistId);
            var result = collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                playlistGetModel = BsonSerializer.Deserialize<PlaylistGetModel>(result);
                playlistGetModel.SongGetModels = new List<SongGetModel>();
                foreach (BsonDocument res in result.GetValue("Songs").AsBsonArray)
                {
                    playlistGetModel.SongGetModels.Add(BsonSerializer.Deserialize<SongGetModel>(res));
                }
            }
            return playlistGetModel;
        }

        //GET ALL PLAYLIST
        public List<PlaylistGetModel> GetAllPlaylist(string creatorId)
        { 
            List<PlaylistGetModel> playlistGetModels = new List<PlaylistGetModel>();
            var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("playlists");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("CreatorId", creatorId);
            var results = collection.Find(filter).ToList();
            int i = 0;
            foreach (BsonDocument result in results)
            {
                if (result != null)
                {
                    playlistGetModels.Add(BsonSerializer.Deserialize<PlaylistGetModel>(result));
                    playlistGetModels.ElementAt(i).SongGetModels = new List<SongGetModel>();
                    foreach (BsonDocument res in result.GetValue("Songs").AsBsonArray)
                    {
                        if (res != null)
                        {
                            playlistGetModels.ElementAt(i).SongGetModels.Add(BsonSerializer.Deserialize<SongGetModel>(res));
                        }
                    }
                }
                i++;
            }
            return playlistGetModels;
        }

        //DELETE ALBUM
        public bool DeletePlaylist(string userId, string playlistId)
        {
            var collection = new MongodbConnectionProvider().GeShantyDatabase().GetCollection<BsonDocument>("playlists");
            var deleteFilter = Builders<BsonDocument>.Filter.Eq("CreatorId", userId) & Builders<BsonDocument>.Filter.Eq("PlaylistId", playlistId);
            if (collection.DeleteOne(deleteFilter).DeletedCount > 0)
            {
                return true;
            }
            return false;
        }
    }
}
