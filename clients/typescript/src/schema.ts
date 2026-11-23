// Generated from openapi/openapi.json by `npm run generate`. Don't edit: rebuild the API, then
// run the script again.

export interface paths {
    "/people": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Search and list people
         * @description Every word of `q` must match, ignoring case and accents. Without `sort`, a search puts the best matches first (a name word starting with the term, then the term elsewhere in the name, then in any other field) and the list is otherwise newest first.
         */
        get: operations["ListPeople"];
        put?: never;
        /** Add a person */
        post: operations["CreatePerson"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/people/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get one person */
        get: operations["GetPerson"];
        /** Replace a person's details */
        put: operations["UpdatePerson"];
        post?: never;
        /** Delete a person */
        delete: operations["DeletePerson"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/people/audit-log": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The audit log, newest first: who added, edited or deleted whom */
        get: operations["GetPeopleAuditLog"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/people/options": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Allowed countries, interests, age ranges and sort orders */
        get: operations["GetPeopleOptions"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/people/summary": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Totals for the whole directory */
        get: operations["GetPeopleSummary"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
}
export type webhooks = Record<string, never>;
export interface components {
    schemas: {
        AgeRangeOption: {
            /** Format: int32 */
            max: null | number;
            /** Format: int32 */
            min: number;
            value: string;
        };
        CountryCount: {
            code: string;
            name: string;
            /** Format: int32 */
            people: number;
        };
        CountryOption: {
            code: string;
            name: string;
        };
        /** @description Totals for the whole directory. */
        DirectorySummary: {
            /**
             * Format: int32
             * @description Their average age, rounded.
             */
            averageAge: number;
            /** @description People per country, by country name. */
            countries: components["schemas"]["CountryCount"][];
            /** @description People per interest, most popular first. */
            interests: components["schemas"]["InterestCount"][];
            /**
             * Format: int32
             * @description How many people there are.
             */
            people: number;
        };
        HttpValidationProblemDetails: {
            detail?: null | string;
            errors?: {
                [key: string]: string[];
            };
            instance?: null | string;
            /** Format: int32 */
            status?: null | number;
            title?: null | string;
            type?: null | string;
        };
        InterestCount: {
            name: string;
            /** Format: int32 */
            people: number;
        };
        /** @description The allowed values of the fields and filters. */
        PeopleOptions: {
            ageRanges: components["schemas"]["AgeRangeOption"][];
            countries: components["schemas"]["CountryOption"][];
            interests: string[];
            /** Format: int32 */
            maxInterests: number;
            sorts: string[];
        };
        /** @description One line of the audit log. */
        PersonAuditEntryDto: {
            action: string;
            /** Format: date-time */
            at: string;
            byName: string;
            client: string;
            /** Format: int64 */
            id: number;
            /** Format: int32 */
            personId: number;
        };
        /** @description A person, as the API returns it. */
        PersonDto: {
            /** Format: int32 */
            age: number;
            bio: null | string;
            city: null | string;
            country: string;
            /** Format: date-time */
            createdAt: string;
            email: string;
            fullName: string;
            /** Format: int32 */
            id: number;
            interests: string[];
            jobTitle: null | string;
            phone: null | string;
            /** Format: date-time */
            updatedAt: string;
        };
        /**
         * @description What a person sent to `POST /people` or `PUT /people/{id}`. Optional text may be
         *     left out, null or empty; it is stored as null.
         */
        PersonInput: {
            /**
             * Format: int32
             * @example 29
             */
            age?: null | number;
            /** @example Builds accessible design systems. */
            bio?: null | string;
            /** @example Manchester */
            city?: null | string;
            /**
             * @description ISO 3166-1 alpha-2 code (see `GET /people/options`).
             * @example GB
             */
            country?: null | string;
            /** @example amara.okafor@example.com */
            email?: null | string;
            /** @example Amara Okafor */
            fullName?: null | string;
            /** @description 1 to 5 of the interests in `GET /people/options`. */
            interests?: null | string[];
            /** @example Frontend developer */
            jobTitle?: null | string;
            /** @example +44 7700 900123 */
            phone?: null | string;
        };
        /** @description One page of the list. */
        PersonPage: {
            /**
             * Format: int32
             * @description The position of the first item on this page, from 1 (0 when nothing matches).
             */
            from: number;
            /** @description The people on this page. */
            items: components["schemas"]["PersonDto"][];
            /**
             * Format: int32
             * @description This page's number, from 1 (out-of-range pages are clamped).
             */
            page: number;
            /**
             * Format: int32
             * @description How many pages there are (at least 1).
             */
            pageCount: number;
            /** @description The normalised search words, for highlighting matches. */
            terms: string[];
            /**
             * Format: int32
             * @description The position of the last item on this page.
             */
            to: number;
            /**
             * Format: int32
             * @description How many people match, on every page.
             */
            total: number;
        };
        ProblemDetails: {
            detail?: null | string;
            instance?: null | string;
            /** Format: int32 */
            status?: null | number;
            title?: null | string;
            type?: null | string;
        };
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    ListPeople: {
        parameters: {
            query?: {
                age?: string;
                country?: string;
                interests?: string[];
                page?: number;
                q?: string;
                size?: number;
                sort?: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PersonPage"];
                };
            };
        };
    };
    CreatePerson: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PersonInput"];
            };
        };
        responses: {
            /** @description Created */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PersonDto"];
                };
            };
            /** @description Bad Request */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            /** @description No valid authentik token. */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Signed in, but not an editor for this app (ask for access on the sign-in page). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Conflict */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    GetPerson: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PersonDto"];
                };
            };
            /** @description Not Found */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    UpdatePerson: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PersonInput"];
            };
        };
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PersonDto"];
                };
            };
            /** @description Bad Request */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            /** @description No valid authentik token. */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Signed in, but not an editor for this app (ask for access on the sign-in page). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Not Found */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
            /** @description Conflict */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    DeletePerson: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description No Content */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description No valid authentik token. */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Signed in, but not an editor for this app (ask for access on the sign-in page). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Not Found */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    GetPeopleAuditLog: {
        parameters: {
            query?: {
                limit?: number;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PersonAuditEntryDto"][];
                };
            };
            /** @description No valid authentik token. */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Signed in, but not an editor for this app (ask for access on the sign-in page). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetPeopleOptions: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PeopleOptions"];
                };
            };
        };
    };
    GetPeopleSummary: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["DirectorySummary"];
                };
            };
        };
    };
}
