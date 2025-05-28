using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers
{
    public static class CategoryMapper
    {
        public static CategoryDto MapToDto(this Category entity)
        {
            return new CategoryDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ParentId = entity.ParentId,
                Icon = entity.Icon,
                Color = entity.Color,
                Subcategories = entity.Subcategories?.Select(MapToDto).ToList() ?? new()
            };
        }

        public static Category MapToEntity(this CategoryDto dto)
        {
            return new Category
            {
                Id = dto.Id,
                Name = dto.Name,
                ParentId = dto.ParentId,
                Icon = dto.Icon,
                Color = dto.Color,
            };
        }

        public static void UpdateEntity(this Category entity, CategoryDto dto)
        {
            entity.Name = dto.Name;
            entity.ParentId = dto.ParentId;
            entity.Icon = dto.Icon;
            entity.Color = dto.Color;
        }
    }
}
