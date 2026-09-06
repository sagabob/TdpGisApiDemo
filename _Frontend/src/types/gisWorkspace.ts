/** Mirrors backend `GisConnectionDto` / JSON (camelCase). */

export interface PropertyMappingDto {
    id: string;
    columnType: number;
    propertyName: string;
    propertyLabel: string;
}

export interface GisConnectionDto {
    id: string;
    name: string;
    entity: string;
    geometryType: number;
    queryField: string;
    propertyMappings: PropertyMappingDto[];
    entityLabel: string;
    description?: string;
    /** Set when entities come from merged public + private workspace lists (see `/api/gis/workspace-entities`). */
    workspaceId?: string;
    /** True when this entity was loaded from the private workspace (signed-in merge). */
    isPrivate?: boolean;
}
