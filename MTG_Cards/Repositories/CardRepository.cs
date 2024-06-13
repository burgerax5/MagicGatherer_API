using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using MTG_Cards.Services.Mappers;

namespace MTG_Cards.Repositories
{
    public class CardRepository : ICardRepository
    {
        private readonly DataContext _context;

        public CardRepository(DataContext context)
        {
            _context = context;
        }

        public bool CardExists(int id)
        {
            return _context.Cards.Any(c => c.Id == id);
        }

        public ICollection<CardDTO> GetCards(int page)
        {
            List<Card> cards = _context.Cards
                .Skip(page * 50)
                .Take(50)
                .Include(c => c.Edition)
                .Include(c => c.Conditions)
                .ToList();
            List<CardDTO> cardsDTO = new List<CardDTO>();
            foreach (Card card in cards)
            {
				CardDTO cardDTO = CardMapper.ToDTO(card);
				cardsDTO.Add(cardDTO);
			}

            return cardsDTO;
        }

        public Card GetCardById(int id)
        {
            var card = _context.Cards
                .Include(c => c.Edition)
                .Include(c => c.Conditions)
                .FirstOrDefault(c => c.Id == id);

            return card;
        }

        public Card GetCardByName(string name)
        {
            return _context.Cards
				.Include(c => c.Edition)
				.Include(c => c.Conditions)
				.FirstOrDefault(cards => cards.Name.Contains(name));
        }

        public async Task<bool> CreateCards(string editionName, ICollection<CardDTO> cards)
        {
			//Check if the edition already exists in the database
			var existingEdition = await _context.Editions.FirstOrDefaultAsync(e => e.Name == editionName);

            if (existingEdition == null)
                return false;

			foreach (CardDTO card in cards)
            {
                await _context.Cards.AddAsync(CardMapper.ToModel(existingEdition, card));
            }

            return await SaveAsync();
        }

        public bool CreateCard(string editionName, Card card)
        {
            //Check if the edition already exists in the database
            var existingEdition = _context.Editions.FirstOrDefault(e => e.Name == editionName);

            if (existingEdition == null)
                return false;

            card.Edition = existingEdition;
            _context.Cards.Add(card);
            return Save();
        }

        public bool RemoveCard(Card card)
        {
            _context.Cards.Remove(card);
            return Save();
        }

        public bool Save()
        {
            var saved = _context.SaveChanges();
            return saved > 0 ? true : false;
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        //public void UpdateCard(intCard card)
        //{

        //}
    }
}
