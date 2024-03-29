# <img src="https://uploads-ssl.webflow.com/5ea5d3315186cf5ec60c3ee4/5edf1c94ce4c859f2b188094_logo.svg" alt="Pip.Services Logo" width="200"> <br/> PostgreSQL components for .NET Changelog

## <a name="3.6.0"></a> 3.6.0 (2023-12-15)

### Breaking Changes
* Migrate to .NET 8.0

## <a name="3.5.0"></a> 3.5.0 (2022-08-04)

### Breaking Changes
* Migrate to .NET 6.0

## <a name="3.4.0"></a> 3.4.0 (2021-09-01)

### Breaking Changes
* Migrate to .NET Core 5.0

## <a name="3.3.3"></a> 3.3.3 (2021-07-15) 

### Features
* Minor fixes

## <a name="3.3.2"></a> 3.3.2 (2021-07-15) 

### Features
* Added support for using connection pooling 

## <a name="3.3.1"></a> 3.3.1 (2021-06-16) 

### Features
* Added tests for non-string keys

## <a name="3.3.0"></a> 3.3.0 (2021-06-15) 

### Features
* Added schemas to PostgresPersistence, IdentifiablePostgresPersistence, IdentifiableJsonPostgresPersistence
* Added _autoGenerateId flag to IdentifiablePostgresPersistence

## <a name="3.2.0"></a> 3.2.0 (2021-06-10) 

### Features
* Updated references as PipServices3.Components have got minor changes

## <a name="3.1.0"></a> 3.1.0 (2021-02-19) 

### Features
* Renamed autoCreateObject to ensureSchema
* Added defineSchema method that shall be overriden in child classes
* Added clearSchema method

### Breaking changes
* Method autoCreateObject is deprecated and shall be renamed to ensureSchema

## <a name="3.0.0-3.0.3"></a> 3.0.0-3.0.3 (2020-01-13)

### Features
* added quotes to identifiers and support datamember attribute

### Bug Fixes
* removed dublicate using
* fixed json filters
* fixed saving to jsonb
* changed interfaces of internal methods

## <a name="3.0.0"></a> 3.0.0 (2020-10-22)

Initial public release

### Features
* Added PostgresConnectionResolver
* Added PostgresConnection
* Added PostgresPersistence
* Added IdentifiablePostgresPersistence

### Bug Fixes
No fixes in this version

