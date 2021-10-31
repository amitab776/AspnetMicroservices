using AutoMapper;
using Basket.API.Entities;
using Basket.API.GrpcService;
using Basket.API.Repositories;
using EventBus.Messages.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Basket.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _repository;
        private readonly DiscountGrpcService _discountGrpcService;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;

        public BasketController(IBasketRepository repository, DiscountGrpcService discountGrpcService, IMapper mapper, IPublishEndpoint publishEndpoint)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _discountGrpcService = discountGrpcService ?? throw new ArgumentNullException(nameof(discountGrpcService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
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
            //TODO: Communicate with Discount.Grpc and
            //Calculate latest price of product into shopping cart.
            foreach(var item in basket.Items)
            {
                var coupon = await _discountGrpcService.GetDiscount(item.ProductName).ConfigureAwait(false);
                item.Price -= coupon.Amount;
            }

            return Ok(await _repository.UpdateBasket(basket).ConfigureAwait(false));
        }

        [HttpDelete]
        [Route("{userName}", Name ="DeleteBasket")]
        public async Task DeleteBasket(string userName)
        {
            await _repository.DeletBasket(userName).ConfigureAwait(false);
        }

        [Route("[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Chckout([FromBody] BasketCheckout basketCheckout)
        {
            //get existing basket with total price
            //Set TotalPrice on basketCheckOutEvent --Set TotalPrice on basketCheckout eventMessage
            //send Checkout event to rabbitmq
            //remove the basket

            //get existing basket with total price
            var basket = await _repository.GetBasket(basketCheckout.UserName).ConfigureAwait(false);
            if(basket == null)
            {
                return BadRequest();
            }

            //send Checkout event to rabbitmq
            var eventMessage = _mapper.Map<BasketCheckOutEvent>(basketCheckout);
            eventMessage.TotalPrice = basket.TotalPrice;
            await _publishEndpoint.Publish(eventMessage).ConfigureAwait(false);

            //remove the basket
            await _repository.DeletBasket(basket.UserName).ConfigureAwait(false);
            return Accepted();
        }
    }
}
