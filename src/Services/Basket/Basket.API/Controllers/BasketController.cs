using Basket.API.Entities;
using Basket.API.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Basket.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _repository;

        public BasketController(IBasketRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet("{userName}", Name = "GetBasket")]
        [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ShoppingCart>> GetBasket(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return BadRequest("Key is expected");
            var basket = await _repository.GetBasket(userName).ConfigureAwait(false);

            return Ok(basket?? new ShoppingCart(userName)); 
        }

        [HttpPost]
        [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> UpdateBasket([FromBody] ShoppingCart basket)
        {
            return Ok(await _repository.UpdateBasket(basket).ConfigureAwait(false));
        }

        [HttpDelete]
        [Route("{userName}", Name ="DeleteBasket")]
        public async Task DeleteBasket(string userName)
        {
            await _repository.DeletBasket(userName).ConfigureAwait(false);
        }
    }
}
