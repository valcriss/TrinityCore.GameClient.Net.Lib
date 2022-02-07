using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace Testing
{
    public class WorldServerAuthentificationTests
    {
        private const string HOSTNAME_GOOD = "danielsilvestre.fr";
        private const int PORT_GOOD = 3724;
        private const string USERNAME_GOOD = "test";
        private const string PASSWORD_GOOD = "test";

        [SetUp]
        public void Setup()
        {
            Logger.RegisterHandler("trace", new NUnitLoggerHandler());
        }

        [Test]
        [Category("WorldServer Authentification")]
        public void AuthLoginSuccess()
        {
            AuthInformation authInformation = LoginAndGetFirstRealm();
            WorldClient client = new WorldClient(authInformation.WorldServer, authInformation.Credentials);
            bool result = client.Authenticate().Result;
            Assert.IsTrue(result);
            client.Close();
        }

        [Test]
        [Category("WorldServer Authentification")]
        public void AuthLoginCharactersList()
        {
            AuthInformation authInformation = LoginAndGetFirstRealm();
            WorldClient client = new WorldClient(authInformation.WorldServer, authInformation.Credentials);
            bool result = client.Authenticate().Result;
            Assert.IsTrue(result);
            List<Character> characters = client.GetCharacters().Result;
            Assert.AreEqual(1, characters.Count);
            client.Close();
        }

        [Test]
        [Category("WorldServer Authentification")]
        public void AuthLoginCharactersInWorld()
        {
            AuthInformation authInformation = LoginAndGetFirstRealm();
            WorldClient client = new WorldClient(authInformation.WorldServer, authInformation.Credentials);
            bool result = client.Authenticate().Result;
            Assert.IsTrue(result);
            List<Character> characters = client.GetCharacters().Result;
            Assert.AreEqual(1, characters.Count);
            bool login = client.LoginCharacter(characters[0]).Result;
            Assert.IsTrue(login);

            client.Close();
        }

        [Test]
        [Category("WorldServer Authentification")]
        public void AuthLoginCharactersInWorldAndLogOut()
        {
            AuthInformation authInformation = LoginAndGetFirstRealm();
            WorldClient client = new WorldClient(authInformation.WorldServer, authInformation.Credentials);
            bool result = client.Authenticate().Result;
            Assert.IsTrue(result);
            List<Character> characters = client.GetCharacters().Result;
            Assert.AreEqual(1, characters.Count);
            bool login = client.LoginCharacter(characters[0]).Result;
            Assert.IsTrue(login);
            bool logout = client.LogOut().Result;
            Assert.IsTrue(logout);
            client.Close();
        }

        private AuthInformation LoginAndGetFirstRealm()
        {
            AuthClient client = new AuthClient(HOSTNAME_GOOD, PORT_GOOD, USERNAME_GOOD, PASSWORD_GOOD);
            bool result = client.Authenticate().Result;
            Assert.IsTrue(result);
            List<WorldServerInfo> realms = client.GetRealms().Result;
            Assert.AreEqual(1, realms.Count);
            return new AuthInformation() { WorldServer = realms[0], Credentials = client.Credentials };

        }
    }

    public class AuthInformation
    {
        public WorldServerInfo WorldServer { get; set; }
        public AuthCredentials Credentials { get; set; }
    }
}
