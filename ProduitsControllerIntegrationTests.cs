using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using API_REST_REVISION.Models;
using API_REST_REVISION.Models.DTO;
using API_REST_Revision;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace API_TEST
{
    [TestClass]
    public class ProduitsControllerIntegrationTests
    {
        private ProduitsDbContext _context = null!;
        private ProduitRepository _repository = null!;
        private ProduitsController _controller = null!;

        [TestInitialize]
        public async Task Setup()
        {
            var dbName = "ProduitsTestDB_" + System.Guid.NewGuid().ToString("N");
            var options = new DbContextOptionsBuilder<ProduitsDbContext>()
                .UseNpgsql($"Server=localhost;port=5432;Database={dbName};uid=postgres;password=postgres;")
                .Options;

            _context = new ProduitsDbContext(options);
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            _repository = new ProduitRepository(_context);
            _controller = new ProduitsController(_repository, mapper);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }

        [TestMethod]
        public async Task PostProduit_And_GetProduit_EndToEnd()
        {
            var marque = new Marque { NomMarque = "Test Marque" };
            var type = new TypeProduit { NomTypeProduit = "Test Type" };
            _context.Marques.Add(marque);
            _context.TypeProduits.Add(type);
            await _context.SaveChangesAsync();

            var newProduit = new Produit 
            { 
                NomProduit = "Test Produit E2E", 
                IdMarque = marque.IdMarque, 
                IdTypeProduit = type.IdTypeProduit,
                StockReel = 10,
                StockMin = 1,
                StockMax = 20
            };

            var postResult = await _controller.PostProduit(newProduit);
            var createdAtActionResult = postResult.Result as CreatedAtActionResult;
            Assert.IsNotNull(createdAtActionResult);
            
            var createdProduit = createdAtActionResult.Value as ProduitDto;
            Assert.IsNotNull(createdProduit);
            Assert.IsTrue(createdProduit.Id > 0);

            var getResult = await _controller.GetProduit(createdProduit.Id);
            var okResult = getResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            
            var retrievedProduit = okResult.Value as ProduitDetailDto;
            Assert.IsNotNull(retrievedProduit);
            Assert.AreEqual(newProduit.NomProduit, retrievedProduit.Nom);
            Assert.AreEqual("Test Marque", retrievedProduit.Marque);
            Assert.AreEqual("Test Type", retrievedProduit.Type);
        }

        [TestMethod]
        public async Task PutProduit_UpdatesSuccessfully()
        {
            var marque = new Marque { NomMarque = "Test Marque" };
            var type = new TypeProduit { NomTypeProduit = "Test Type" };
            _context.Marques.Add(marque);
            _context.TypeProduits.Add(type);
            await _context.SaveChangesAsync();

            var produit = new Produit 
            { 
                NomProduit = "Original Name", 
                IdMarque = marque.IdMarque, 
                IdTypeProduit = type.IdTypeProduit,
                StockReel = 10, 
                StockMin = 0, 
                StockMax = 100
            };
            _context.Produits.Add(produit);
            await _context.SaveChangesAsync();
            
            _context.ChangeTracker.Clear();

            var updatedProduit = new Produit
            {
                IdProduit = produit.IdProduit,
                NomProduit = "Updated Name",
                IdMarque = produit.IdMarque,
                IdTypeProduit = produit.IdTypeProduit,
                StockReel = 50, 
                StockMin = 0, 
                StockMax = 100
            };

            var putResult = await _controller.PutProduit(updatedProduit.IdProduit, updatedProduit);
            Assert.IsInstanceOfType(putResult, typeof(NoContentResult));

            var getResult = await _controller.GetProduit(updatedProduit.IdProduit);
            var okResult = getResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            
            var retrievedProduit = okResult.Value as ProduitDetailDto;
            Assert.IsNotNull(retrievedProduit);
            Assert.AreEqual("Updated Name", retrievedProduit.Nom);
        }

        [TestMethod]
        public async Task DeleteProduit_DeletesSuccessfully()
        {
            var marque = new Marque { NomMarque = "Test Marque" };
            var type = new TypeProduit { NomTypeProduit = "Test Type" };
            _context.Marques.Add(marque);
            _context.TypeProduits.Add(type);
            await _context.SaveChangesAsync();

            var produit = new Produit 
            { 
                NomProduit = "To Delete", 
                IdMarque = marque.IdMarque, 
                IdTypeProduit = type.IdTypeProduit,
                StockReel = 10,
                StockMin = 0,
                StockMax = 100
            };
            _context.Produits.Add(produit);
            await _context.SaveChangesAsync();
            
            _context.ChangeTracker.Clear();

            var deleteResult = await _controller.DeleteProduit(produit.IdProduit);
            Assert.IsInstanceOfType(deleteResult, typeof(NoContentResult));

            var getResult = await _controller.GetProduit(produit.IdProduit);
            Assert.IsInstanceOfType(getResult.Result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task PostProduit_InvalidMarque_ThrowsDbUpdateException()
        {
            var type = new TypeProduit { NomTypeProduit = "Test Type" };
            _context.TypeProduits.Add(type);
            await _context.SaveChangesAsync();

            var produit = new Produit 
            { 
                NomProduit = "Invalid Produit", 
                IdMarque = 99999, 
                IdTypeProduit = type.IdTypeProduit,
                StockReel = 10,
                StockMin = 0,
                StockMax = 100
            };

            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () =>
            {
                await _controller.PostProduit(produit);
            });
        }
    }
}
