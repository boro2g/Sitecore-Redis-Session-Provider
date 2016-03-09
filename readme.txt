<!--web.config-->
<sessionState customProvider="redis" mode="Custom" cookieless="false" timeout="1" sessionIDManagerType="Sitecore.SessionManagement.ConditionalSessionIdManager">
  <providers>
    <add name="redis" type="TrueClarity.SessionProvider.Redis.SitecoreRedisSessionProvider, TrueClarity.SessionProvider.Redis" sessionType="private" host="localhost" timeout="1" port="6379" accessKey="" ssl="false" pollingInterval="2" compression="true" />
  </providers>
</sessionState>

<!--Sitecore.Analytics.Tracking.config-->
<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/" xmlns:set="http://www.sitecore.net/xmlconfig/set/">
  <sitecore>
    <tracking>
      <sharedSessionState>
        <patch:delete />
      </sharedSessionState>
      <sharedSessionState>
        <providers>
          <clear/>
          <add
            name="redisShared"
            type="TrueClarity.SessionProvider.Redis.SitecoreRedisSessionProvider, TrueClarity.SessionProvider.Redis"
            host="localhost"
            port="6379"
            accessKey=""
            ssl="false"
            timeout="1"
            pollingInterval="2"
            compression="true"
            sessionType="shared"/>
        </providers>

        <manager type="Sitecore.Analytics.Tracking.SharedSessionState.SharedSessionStateManager, Sitecore.Analytics">
          <param desc="configuration" ref="tracking/sharedSessionState/config" />
        </manager>

        <config type="Sitecore.Analytics.Tracking.SharedSessionState.SharedSessionStateConfig, Sitecore.Analytics">
          <param desc="maxLockAge">5000</param>
        </config>
      </sharedSessionState>
    </tracking>
  </sitecore>
</configuration>