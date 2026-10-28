export interface Brand {
    id: number;
    name: string;
    description: string | null;
    isActive: boolean;
}

export interface Unit {
    id: number;
    name: string;
    shortName: string | null;
    description: string | null;
    isActive: boolean;
}

export interface Tax {
    id: number;
    name: string;
    percentage: number;
    description: string | null;
    isActive: boolean;
}
