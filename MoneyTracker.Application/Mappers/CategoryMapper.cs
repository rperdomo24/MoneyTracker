using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;

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
                Icon = Enum.TryParse<CategoryIcon>(entity.Icon, out var parsedIcon) ? parsedIcon : CategoryIcon.Payments,
                Color = entity.Color,
                Type = entity.Type,
                Subcategories = entity.Children?.Select(MapToDto).ToList() ?? new()
            };
        }

        public static Category MapToEntity(this CategoryDto dto)
        {
            return new Category
            {
                Id = dto.Id,
                Name = dto.Name,
                ParentId = dto.ParentId,
                Icon = dto.Icon.ToString(),
                Color = dto.Color,
                Type = dto.Type,
                Children = dto.Subcategories?.Select(MapToEntity).ToList() ?? new()
            };
        }

        public static void UpdateEntity(this Category entity, CategoryDto dto)
        {
            entity.Name = dto.Name;
            entity.ParentId = dto.ParentId;
            entity.Icon = dto.Icon.ToString();
            entity.Color = dto.Color;
            entity.Type = dto.Type;
        }
    }
}
