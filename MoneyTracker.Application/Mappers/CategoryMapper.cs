using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Application.Mappers
{
    public static class CategoryMapper
    {
     
        public static CategoryDto MapToDto(this Category entity)
        {
            if (entity == null)
                return new CategoryDto();

            return new CategoryDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ParentId = entity.ParentId,
                Type = entity.Type,
                Icon = Enum.TryParse<CategoryIcon>(entity.Icon, out var parsedIcon)
                    ? parsedIcon
                    : CategoryIcon.Payments,
                Color = entity.Color,
                IsSystem = entity.IsSystem,
                SystemCategoryCode = entity.SystemCategoryCode,
                UpdatedAt = entity.UpdatedAt,
                Parent = null,

                Children = entity.Children?.Select(child => new CategoryDto
                {
                    Id = child.Id,
                    Name = child.Name,
                    ParentId = child.ParentId,
                    Type = child.Type,
                    Icon = Enum.TryParse<CategoryIcon>(child.Icon, out var parsedChildIcon)
                        ? parsedChildIcon
                        : CategoryIcon.Payments,
                    Color = child.Color,
                    IsSystem = child.IsSystem,
                    SystemCategoryCode = child.SystemCategoryCode,
                    UpdatedAt = child.UpdatedAt,
                    Parent = null,
                    Children = child.Children?.Select(MapToDto).ToList() ?? new()
                }).ToList() ?? new()
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
                IsSystem = dto.IsSystem,
                SystemCategoryCode = dto.SystemCategoryCode,
                UpdatedAt = dto.UpdatedAt,
                Children = dto.Children?.Select(MapToEntity).ToList() ?? new()
            };
        }

        public static void UpdateEntity(this Category entity, CategoryDto dto)
        {
            entity.Name = dto.Name;
            entity.ParentId = dto.ParentId;
            entity.Icon = dto.Icon.ToString();
            entity.Color = dto.Color;
            entity.Type = dto.Type;
            entity.IsSystem = dto.IsSystem;
            entity.SystemCategoryCode = dto.SystemCategoryCode;
            entity.UpdatedAt = dto.UpdatedAt;
        }
    }
}
