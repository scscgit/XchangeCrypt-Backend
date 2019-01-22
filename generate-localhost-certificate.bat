dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p crypticpassword
dotnet dev-certs https --trust
@pause