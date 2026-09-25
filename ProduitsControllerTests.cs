using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using API_REST_REVISION.Models;
using API_REST_REVISION.Models.DTO;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace API_TEST
{
    [TestClass]
    public class ProduitsControllerTests
    {
        private Mock<IProduitRepository> _mockRepository;
        private ProduitsController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IProduitRepository>();
            
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            _controller = new ProduitsController(_mockRepository.Object, mapper);
        }

        [TestMethod]
        public async Task GetProduit_ExistingId_ReturnsOkResult()
        {
            int testId = 1;
            var fakeProduit = new Produit { IdProduit = testId };
            _mockRepository.Setup(repo => repo.GetByIdWithDetailsAsync(testId)).ReturnsAsync(fakeProduit);
           
            var actionResult = await _controller.GetProduit(testId);
            
            var okResult = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult, "Le resultat doit etre de type OkObjectResult.");
            Assert.AreEqual(200, okResult.StatusCode);
            
            var dto = okResult.Value as ProduitDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(testId, dto.Id);
        }

        [TestMethod]
        public async Task GetProduit_UnknownId_ReturnsNotFoundResult()
        {
            int testId = 999;
            _mockRepository.Setup(repo => repo.GetByIdWithDetailsAsync(testId)).ReturnsAsync((Produit)null);
           
            var actionResult = await _controller.GetProduit(testId);

            var notFoundResult = actionResult.Result as NotFoundResult;
            Assert.IsNotNull(notFoundResult);
            Assert.AreEqual(404, notFoundResult.StatusCode);
        }

        [TestMethod]
        public async Task PostProduit_ValidProduct_ReturnsCreatedAtAction()
        {
            var newProduit = new Produit { IdProduit = 2 };
            var actionResult = await _controller.PostProduit(newProduit);
  
            var createdResult = actionResult.Result as CreatedAtActionResult;
            Assert.IsNotNull(createdResult);
            Assert.AreEqual(201, createdResult.StatusCode);
            Assert.AreEqual(nameof(ProduitsController.GetProduit), createdResult.ActionName);

            _mockRepository.Verify(repo => repo.AddAsync(newProduit), Times.Once);
            _mockRepository.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [TestMethod]
        public async Task PutProduit_IdMismatch_ReturnsBadRequest()
        {
            int urlId = 1;
            var produitToUpdate = new Produit { IdProduit = 2 };
            var result = await _controller.PutProduit(urlId, produitToUpdate);
            
            var badRequestResult = result as BadRequestResult;
            Assert.IsNotNull(badRequestResult);
            Assert.AreEqual(400, badRequestResult.StatusCode);
    
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Produit>()), Times.Never);
            _mockRepository.Verify(repo => repo.SaveAsync(), Times.Never);
        }

        [TestMethod]
        public async Task DeleteProduit_ExistingId_ReturnsNoContent()
        {
            int testId = 1;
            var fakeProduit = new Produit { IdProduit = testId };
            _mockRepository.Setup(repo => repo.GetByIdAsync(testId)).ReturnsAsync(fakeProduit);
            
            var result = await _controller.DeleteProduit(testId);
        
            var noContentResult = result as NoContentResult;
            Assert.IsNotNull(noContentResult);
            Assert.AreEqual(204, noContentResult.StatusCode);

            _mockRepository.Verify(repo => repo.DeleteAsync(fakeProduit), Times.Once);
            _mockRepository.Verify(repo => repo.SaveAsync(), Times.Once);
        }
    }
}
