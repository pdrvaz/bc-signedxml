﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace Org.BouncyCastle.Crypto.Xml
{
    public class DSAKeyValue : KeyInfoClause
    {
        private DsaPublicKeyParameters _key;

        //
        // public constructors
        //

        public DSAKeyValue()
        {
            _key = null;
        }

        public DSAKeyValue(DsaPublicKeyParameters key)
        {
            _key = key;
        }

        //
        // public properties
        //

        public DsaPublicKeyParameters Key
        {
            get { return _key; }
            set { _key = value; }
        }

        //
        // public methods
        //

        /// <summary>
        /// Create an XML representation.
        /// </summary>
        /// <remarks>
        /// Based upon https://www.w3.org/TR/xmldsig-core/#sec-DSAKeyValue. 
        /// </remarks>
        /// <returns>
        /// An <see cref="XmlElement"/> containing the XML representation.
        /// </returns>
        public override XmlElement GetXml()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            return GetXml(xmlDocument);
        }

        private const string KeyValueElementName = "KeyValue";
        private const string DSAKeyValueElementName = "DSAKeyValue";

        //Optional {P,Q}-Sequence
        private const string PElementName = "P";
        private const string QElementName = "Q";

        //Optional Members
        private const string GElementName = "G";
        private const string JElementName = "J";

        //Mandatory Members
        private const string YElementName = "Y";

        //Optional {Seed,PgenCounter}-Sequence
        private const string SeedElementName = "Seed";
        private const string PgenCounterElementName = "PgenCounter";

        internal override XmlElement GetXml(XmlDocument xmlDocument)
        {
            XmlElement keyValueElement = xmlDocument.CreateElement(KeyValueElementName, SignedXml.XmlDsigNamespaceUrl);
            XmlElement dsaKeyValueElement = xmlDocument.CreateElement(DSAKeyValueElementName, SignedXml.XmlDsigNamespaceUrl);

            XmlElement pElement = xmlDocument.CreateElement(PElementName, SignedXml.XmlDsigNamespaceUrl);
            pElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(_key.Parameters.P.ToByteArray())));
            dsaKeyValueElement.AppendChild(pElement);

            XmlElement qElement = xmlDocument.CreateElement(QElementName, SignedXml.XmlDsigNamespaceUrl);
            qElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(_key.Parameters.Q.ToByteArray())));
            dsaKeyValueElement.AppendChild(qElement);

            XmlElement gElement = xmlDocument.CreateElement(GElementName, SignedXml.XmlDsigNamespaceUrl);
            gElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(_key.Parameters.G.ToByteArray())));
            dsaKeyValueElement.AppendChild(gElement);

            XmlElement yElement = xmlDocument.CreateElement(YElementName, SignedXml.XmlDsigNamespaceUrl);
            yElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(_key.Y.ToByteArray())));
            dsaKeyValueElement.AppendChild(yElement);

            // Add optional components if present
            /*if (dsaParams.J != null)
            {
                XmlElement jElement = xmlDocument.CreateElement(JElementName, SignedXml.XmlDsigNamespaceUrl);
                jElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.J)));
                dsaKeyValueElement.AppendChild(jElement);
            }*/

            //if (dsaParams.Seed != null)
            //{  // note we assume counter is correct if Seed is present
                XmlElement seedElement = xmlDocument.CreateElement(SeedElementName, SignedXml.XmlDsigNamespaceUrl);
                seedElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(_key.Parameters.ValidationParameters.GetSeed())));
                dsaKeyValueElement.AppendChild(seedElement);

                XmlElement counterElement = xmlDocument.CreateElement(PgenCounterElementName, SignedXml.XmlDsigNamespaceUrl);
                counterElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(Utils.ConvertIntToByteArray(_key.Parameters.ValidationParameters.Counter))));
                dsaKeyValueElement.AppendChild(counterElement);
            //}

            keyValueElement.AppendChild(dsaKeyValueElement);

            return keyValueElement;
        }

        /// <summary>
        /// Deserialize from the XML representation.
        /// </summary>
        /// <remarks>
        /// Based upon https://www.w3.org/TR/xmldsig-core/#sec-DSAKeyValue. 
        /// </remarks>
        /// <param name="value">
        /// An <see cref="XmlElement"/> containing the XML representation. This cannot be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> cannot be null.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The XML has the incorrect schema or the DSA parameters are invalid.
        /// </exception>
        public override void LoadXml(XmlElement value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (value.Name != KeyValueElementName
                || value.NamespaceURI != SignedXml.XmlDsigNamespaceUrl)
            {
                throw new System.Security.Cryptography.CryptographicException(String.Format("Root element must be {KeyValueElementName} element in namepsace {SignedXml.XmlDsigNamespaceUrl}"));
            }

            const string xmlDsigNamespacePrefix = "dsig";
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            xmlNamespaceManager.AddNamespace(xmlDsigNamespacePrefix, SignedXml.XmlDsigNamespaceUrl);

            XmlNode dsaKeyValueElement = value.SelectSingleNode(String.Format("{xmlDsigNamespacePrefix}:{DSAKeyValueElementName}"), xmlNamespaceManager);
            if (dsaKeyValueElement == null)
            {
                throw new System.Security.Cryptography.CryptographicException(String.Format("{KeyValueElementName} must contain child element {DSAKeyValueElementName}"));
            }

            XmlNode yNode = dsaKeyValueElement.SelectSingleNode(String.Format("{xmlDsigNamespacePrefix}:{YElementName}"), xmlNamespaceManager);
            if (yNode == null)
                throw new System.Security.Cryptography.CryptographicException(String.Format("{YElementName} is missing"));

            XmlNode pNode = dsaKeyValueElement.SelectSingleNode(String.Format("{xmlDsigNamespacePrefix}:{PElementName}"), xmlNamespaceManager);
            XmlNode qNode = dsaKeyValueElement.SelectSingleNode(String.Format("{xmlDsigNamespacePrefix}:{QElementName}"), xmlNamespaceManager);

            if ((pNode == null && qNode != null) || (pNode != null && qNode == null))
                throw new System.Security.Cryptography.CryptographicException(String.Format("{PElementName} and {QElementName} can only occour in combination"));


            XmlNode gNode = dsaKeyValueElement.SelectSingleNode(String.Format("{xmlDsigNamespacePrefix}:{GElementName}"), xmlNamespaceManager);
            XmlNode jNode = dsaKeyValueElement.SelectSingleNode(String.Format("{xmlDsigNamespacePrefix}:{JElementName}"), xmlNamespaceManager);

            XmlNode seedNode = dsaKeyValueElement.SelectSingleNode(String.Format("{xmlDsigNamespacePrefix}:{SeedElementName}"), xmlNamespaceManager);
            XmlNode pgenCounterNode = dsaKeyValueElement.SelectSingleNode(String.Format("{xmlDsigNamespacePrefix}:{PgenCounterElementName}"), xmlNamespaceManager);
            if ((seedNode == null && pgenCounterNode != null) || (seedNode != null && pgenCounterNode == null))
                throw new System.Security.Cryptography.CryptographicException(String.Format("{SeedElementName} and {PgenCounterElementName} can only occur in combination"));

            try
            {
                _key = new DsaPublicKeyParameters(new Math.BigInteger(Convert.FromBase64String(yNode.InnerText)),
                    new DsaParameters(
                        new Math.BigInteger((pNode != null) ? Convert.FromBase64String(pNode.InnerText) : null),
                        new Math.BigInteger((qNode != null) ? Convert.FromBase64String(qNode.InnerText) : null),
                        new Math.BigInteger((gNode != null) ? Convert.FromBase64String(gNode.InnerText) : null),
                        new DsaValidationParameters(
                            (seedNode != null) ? Convert.FromBase64String(seedNode.InnerText) : null,
                            (pgenCounterNode != null) ? Utils.ConvertByteArrayToInt(Convert.FromBase64String(pgenCounterNode.InnerText)) : 0)));
            }
            catch (Exception ex)
            {
                throw new System.Security.Cryptography.CryptographicException("An error occurred parsing the key components", ex);
            }
        }
    }
}
