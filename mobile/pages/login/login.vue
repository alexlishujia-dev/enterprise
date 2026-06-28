<template>
  <view class="login-page">
    <view class="login-page__hero" :style="{ paddingTop: statusBarHeight + 'px' }">
      <view class="login-page__logo">EP</view>
      <view class="login-page__title">EnterprisePlatform</view>
      <view class="login-page__subtitle">企业平台移动端</view>
    </view>

    <view class="login-page__form">
      <text class="login-page__form-title">账号登录</text>
      <u-form :model="form" label-position="top">
        <u-form-item label="用户名" prop="userName" border-bottom>
          <u-input v-model="form.userName" placeholder="请输入用户名" clearable />
        </u-form-item>
        <u-form-item label="密码" prop="password" border-bottom>
          <u-input v-model="form.password" type="password" placeholder="请输入密码" clearable />
        </u-form-item>
      </u-form>

      <u-button
        type="primary"
        shape="circle"
        :loading="loading"
        text="登录"
        custom-style="margin-top: 48rpx; height: 88rpx;"
        @click="handleLogin" />
      <view class="login-page__hint">默认账号：admin / Admin@123</view>
    </view>
  </view>
</template>

<script setup>
import { reactive, ref } from 'vue';
import { onLoad } from '@dcloudio/uni-app';
import { useUserStore } from '@/store/user';
import { getStatusBarHeight } from '@/utils/device';

const userStore = useUserStore();
const statusBarHeight = getStatusBarHeight() + 24;
const loading = ref(false);
const form = reactive({
  userName: 'admin',
  password: 'Admin@123'
});

onLoad(() => {
  userStore.hydrateFromStorage();
  if (userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/index/index' });
  }
});

async function handleLogin() {
  if (!form.userName.trim() || !form.password) {
    uni.showToast({ title: '请输入用户名和密码', icon: 'none' });
    return;
  }

  loading.value = true;
  try {
    await userStore.login({
      userName: form.userName.trim(),
      password: form.password
    });
    uni.showToast({ title: '登录成功', icon: 'success' });
    setTimeout(() => uni.reLaunch({ url: '/pages/index/index' }), 300);
  } catch (err) {
    uni.showToast({ title: err.message || '登录失败', icon: 'none' });
  } finally {
    loading.value = false;
  }
}
</script>

<style scoped lang="scss">
.login-page {
  min-height: 100vh;
  background: linear-gradient(180deg, #1677ff 0%, #f5f6f8 45%);

  &__hero {
    padding: 80rpx $page-padding 60rpx;
    color: #fff;
    text-align: center;
  }

  &__logo {
    width: 120rpx;
    height: 120rpx;
    margin: 0 auto 24rpx;
    border-radius: 28rpx;
    background: rgba(255, 255, 255, 0.2);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 48rpx;
    font-weight: 700;
  }

  &__title {
    font-size: 44rpx;
    font-weight: 700;
  }

  &__subtitle {
    margin-top: 12rpx;
    font-size: 26rpx;
    opacity: 0.9;
  }

  &__form {
    margin: 0 $page-padding;
    padding: 48rpx $page-padding 56rpx;
    background: #fff;
    border-radius: 24rpx;
    box-shadow: 0 12rpx 40rpx rgba(22, 119, 255, 0.12);
  }

  &__form-title {
    display: block;
    margin-bottom: 32rpx;
    font-size: 36rpx;
    font-weight: 600;
    color: $uni-text-color;
  }

  &__hint {
    margin-top: 24rpx;
    text-align: center;
    font-size: 24rpx;
    color: $uni-text-color-secondary;
  }
}
</style>
