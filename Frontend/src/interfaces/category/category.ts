// Matches Billing.Application.DTOs.Categories.CategoryDto
export interface Category {
    id: number;
    name: string;
    description: string | null;
    isActive: boolean;
}

// Matches Billing.Application.DTOs.Category.SaveCategoryRequest
export interface SaveCategoryRequest {
    name: string;
    description: string;
    isActive: boolean;
}
