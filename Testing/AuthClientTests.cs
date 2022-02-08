using NUnit.Framework;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;

namespace Testing
{
    public class AuthClientTests
    {
        private const string HOSTNAME_GOOD = "danielsilvestre.fr";
        private const string HOSTNAME_BAD = "danielsilvestre2.fr";
        private const int PORT_GOOD = 3724;
        private const int PORT_BAD = 3725;
        private const string USERNAME_GOOD = "test";
        private const string USERNAME_BAD = "bad";
        private const string PASSWORD_GOOD = "test";
        private const string PASSWORD_BAD = "bad";


        [OneTimeSetUp]
        public void Setup()
        {
            Logger.RegisterHandler("trace",new NUnitLoggerHandler());
        }

        [Test]
        [Category("AuthServer")]
        public void AuthLoginSuccess()
        {
            AuthClient client = new AuthClient();
            bool result = client.Authenticate(HOSTNAME_GOOD, PORT_GOOD, USERNAME_GOOD, PASSWORD_GOOD).Result;
            Assert.IsTrue(result);
        }

        [Test]
        [Category("AuthServer")]
        public void AuthLoginFailed()
        {
            AuthClient client = new AuthClient();
            bool result = client.Authenticate(HOSTNAME_GOOD, PORT_GOOD, USERNAME_BAD, PASSWORD_BAD).Result;
            Assert.IsFalse(result);
        }

        [Test]
        [Category("AuthServer")]
        public void AuthLoginTimeOut()
        {
            AuthClient client = new AuthClient();
            bool result = client.Authenticate(HOSTNAME_BAD, PORT_GOOD, USERNAME_GOOD, PASSWORD_GOOD).Result;
            Assert.IsFalse(result);
        }

        [Test]
        [Category("AuthServer")]
        public void AuthLoginBadPort()
        {
            AuthClient client = new AuthClient();
            bool result = client.Authenticate(HOSTNAME_GOOD, PORT_BAD, USERNAME_GOOD, PASSWORD_GOOD).Result;
            Assert.IsFalse(result);
        }

        [Test]
        [Category("AuthServer")]
        public void AuthRealmsListNotLogedIn()
        {
            AuthClient client = new AuthClient();
            List<WorldServerInfo> result = client.GetRealms().Result;
            Assert.IsNull(result);
        }

        [Test]
        [Category("AuthServer")]
        public void AuthRealmsListLoginSuccess()
        {
            AuthClient client = new AuthClient();
            bool result = client.Authenticate(HOSTNAME_GOOD, PORT_GOOD, USERNAME_GOOD, PASSWORD_GOOD).Result;
            Assert.IsTrue(result);
            List<WorldServerInfo> realms = client.GetRealms().Result;
            Assert.AreEqual(1,realms.Count);
        }
    }
}