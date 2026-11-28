// Generated from openapi/openapi.json by `npm run generate`. Don't edit: rebuild the API, then
// run the script again.

export interface paths {
    "/users": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * List users
         * @description Every word of `q` must match, ignoring case and accents. Without `sort`, a search puts the best matches first (a name word starting with the term, then the term elsewhere in the name, then in any other field) and the list is otherwise newest first.
         */
        get: operations["ListUsers"];
        put?: never;
        /** Add user */
        post: operations["CreateUser"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get user */
        get: operations["GetUser"];
        /**
         * Update user
         * @description Replaces all of a user's details.
         */
        put: operations["UpdateUser"];
        post?: never;
        /** Delete user */
        delete: operations["DeleteUser"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/audit-log": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Get audit log
         * @description Newest first: who added, edited or deleted whom.
         */
        get: operations["GetUsersAuditLog"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/options": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Get options
         * @description Allowed countries, interests, age ranges and sort orders.
         */
        get: operations["GetUsersOptions"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/summary": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Get summary
         * @description Totals for the whole directory.
         */
        get: operations["GetUsersSummary"];
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
            users: number;
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
            /** @description Users per country, by country name. */
            countries: components["schemas"]["CountryCount"][];
            /** @description Users per interest, most popular first. */
            interests: components["schemas"]["InterestCount"][];
            /**
             * Format: int32
             * @description How many users there are.
             */
            users: number;
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
            users: number;
        };
        ProblemDetails: {
            detail?: null | string;
            instance?: null | string;
            /** Format: int32 */
            status?: null | number;
            title?: null | string;
            type?: null | string;
        };
        /** @description One line of the audit log. */
        UserAuditEntryDto: {
            action: string;
            /** Format: date-time */
            at: string;
            byName: string;
            client: string;
            /** Format: int64 */
            id: number;
            /** Format: int32 */
            userId: number;
        };
        /** @description A user, as the API returns it. */
        UserDto: {
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
         * @description What a user sent to `POST /users` or `PUT /users/{id}`. Optional text may be
         *     left out, null or empty; it is stored as null.
         */
        UserInput: {
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
             * @description ISO 3166-1 alpha-2 code (see `GET /users/options`).
             * @example GB
             */
            country?: null | string;
            /** @example amara.okafor@example.com */
            email?: null | string;
            /** @example Amara Okafor */
            fullName?: null | string;
            /** @description 1 to 5 of the interests in `GET /users/options`. */
            interests?: null | string[];
            /** @example Frontend developer */
            jobTitle?: null | string;
            /** @example +44 7700 900123 */
            phone?: null | string;
        };
        /** @description The allowed values of the fields and filters. */
        UserOptions: {
            ageRanges: components["schemas"]["AgeRangeOption"][];
            countries: components["schemas"]["CountryOption"][];
            interests: string[];
            /** Format: int32 */
            maxInterests: number;
            sorts: string[];
        };
        /** @description One page of the list. */
        UserPage: {
            /**
             * Format: int32
             * @description The position of the first item on this page, from 1 (0 when nothing matches).
             */
            from: number;
            /** @description The users on this page. */
            items: components["schemas"]["UserDto"][];
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
             * @description How many users match, on every page.
             */
            total: number;
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
    ListUsers: {
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
                    "application/json": components["schemas"]["UserPage"];
                };
            };
        };
    };
    CreateUser: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["UserInput"];
            };
        };
        responses: {
            /** @description Created */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["UserDto"];
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
    GetUser: {
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
                    "application/json": components["schemas"]["UserDto"];
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
    UpdateUser: {
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
                "application/json": components["schemas"]["UserInput"];
            };
        };
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["UserDto"];
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
    DeleteUser: {
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
    GetUsersAuditLog: {
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
                    "application/json": components["schemas"]["UserAuditEntryDto"][];
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
    GetUsersOptions: {
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
                    "application/json": components["schemas"]["UserOptions"];
                };
            };
        };
    };
    GetUsersSummary: {
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
