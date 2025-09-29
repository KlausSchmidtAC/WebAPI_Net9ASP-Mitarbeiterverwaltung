namespace WebAPI_NET9Tests;
using NSubstitute;
using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
using WebAPI_NET9.Controllers;
using Application;
using Domain;
public class MitarbeiterControllerTests

{
    private MitarbeiterController _controller;
    private IMitarbeiterService _service;

    [SetUp]
    public void Setup()
    {
        _service = Substitute.For<IMitarbeiterService>();
        _controller = new MitarbeiterController(_service);
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}
