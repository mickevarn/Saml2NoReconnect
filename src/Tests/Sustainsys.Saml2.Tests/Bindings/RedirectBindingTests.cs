﻿using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using NSubstitute;
using Sustainsys.Saml2.Bindings;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Xunit;

namespace Sustainsys.Saml2.Tests.Bindings;

public class RedirectBindingTests
{
    private const string Xml = "<xml />";

    [Fact]
    public async Task Bind()
    {
        var xd = new XmlDocument();
        xd.LoadXml(Xml);

        var message = new Saml2Message
        {
            Name = "SamlRequest",
            Xml = xd,
            Destination = "https://example.com/destination",
            RelayState = "someRelayState"
        };

        var subject = new RedirectBinding();

        var httpResponse = Substitute.For<HttpResponse>();

        await subject.Bind(httpResponse, message);

        void validateUrl(string url)
        {
            url.Should().StartWith(message.Destination);

            Uri uri = new Uri(url);

            var query = uri.Query.Split("&");

            var expectedParam = $"{message!.Name}=";

            query[0].StartsWith(expectedParam).Should().BeTrue();

            var value = query[0][expectedParam.Length..];

            using var inflated = new MemoryStream(Convert.FromBase64String(Uri.UnescapeDataString(value)));
            using var deflateStream = new DeflateStream(inflated, CompressionMode.Decompress);
            using var reader = new StreamReader(deflateStream);

            reader.ReadToEnd().Should().Be(Xml);

            query[1].Should().Be("RelayState=someRelayState");
        }

        httpResponse.Received().Redirect(Arg.Do<string>(validateUrl));
    }
}
