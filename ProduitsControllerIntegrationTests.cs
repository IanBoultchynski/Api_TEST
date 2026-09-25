using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using API_REST_REVISION.Models;
using API_REST_Revision;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            _repository = new ProduitRepository(_context);
            _controller = new ProduitsController(_repository);
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

            //  POST
            var createdAtActionResult = postResult.Result as CreatedAtActionResult;
            Assert.IsNotNull(createdAtActionResult, "Result should be CreatedAtActionResult (code 201)");
            var createdProduit = createdAtActionResult.Value as Produit;
            Assert.IsNotNull(createdProduit);
            Assert.IsTrue(createdProduit.IdProduit > 0, "L'identifiant auto-généré doit être supérieur à 0");

            // GET
            var getResult = await _controller.GetProduit(createdProduit.IdProduit);

            
            var okResult = getResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult, "Result should be 200 OK");
            var retrievedProduit = okResult.Value as Produit;
            Assert.IsNotNull(retrievedProduit);
            Assert.AreEqual(newProduit.NomProduit, retrievedProduit.NomProduit);
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

         
            Assert.IsInstanceOfType(putResult, typeof(NoContentResult), "Result should be 204 NoContent");

       
            var getResult = await _controller.GetProduit(updatedProduit.IdProduit);
            var okResult = getResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult, "Le produit mis à jour devrait être trouvé (200 OK)");
            var retrievedProduit = okResult.Value as Produit;
            Assert.IsNotNull(retrievedProduit);
            Assert.AreEqual("Updated Name", retrievedProduit.NomProduit);
            Assert.AreEqual(50, retrievedProduit.StockReel);
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

        
            Assert.IsInstanceOfType(deleteResult, typeof(NoContentResult), "Result should be 204 NoContent");

        
            var getResult = await _controller.GetProduit(produit.IdProduit);
            Assert.IsInstanceOfType(getResult.Result, typeof(NotFoundResult), "Le produit supprimé ne devrait plus être trouvé (404 NotFound)");
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
            }, "Une DbUpdateException devrait être levée par PostgreSQL pour violation de contrainte d'intégrité (clé étrangère)");
        }
    }
}
