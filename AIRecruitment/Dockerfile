FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/AIRecruitment.Domain/AIRecruitment.Domain.csproj src/AIRecruitment.Domain/
COPY src/AIRecruitment.Application/AIRecruitment.Application.csproj src/AIRecruitment.Application/
COPY src/AIRecruitment.Infrastructure/AIRecruitment.Infrastructure.csproj src/AIRecruitment.Infrastructure/
COPY src/AIRecruitment.Presentation/AIRecruitment.Presentation.csproj src/AIRecruitment.Presentation/
RUN dotnet restore src/AIRecruitment.Presentation/AIRecruitment.Presentation.csproj

COPY src/ src/
RUN dotnet publish src/AIRecruitment.Presentation/AIRecruitment.Presentation.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIRecruitment.Presentation.dll"]
