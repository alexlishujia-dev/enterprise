<script setup>
import { onLaunch, onShow } from '@dcloudio/uni-app';
import { useUserStore } from '@/store/user';

onLaunch(() => {
  const userStore = useUserStore();
  userStore.hydrateFromStorage();
});

onShow(() => {
  // #ifdef APP-PLUS
  plus.navigator.setStatusBarStyle('dark');
  // #endif
});
</script>

<style lang="scss">
@import 'uview-plus/index.scss';
@import '@/styles/global.scss';
</style>
