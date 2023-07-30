FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ICWBatchUploader/ICWBatchUploader.fsproj", "ICWBatchUploader/"]
RUN dotnet restore "ICWBatchUploader/ICWBatchUploader.fsproj"
COPY . .
WORKDIR "/src/ICWBatchUploader"
RUN dotnet build "ICWBatchUploader.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ICWBatchUploader.fsproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ICWBatchUploader.dll"]
