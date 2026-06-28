<template>
  <view class="page">
    <view class="search-wrap">
      <u-search
        v-model="keyword"
        placeholder="搜索用户名"
        show-action
        action-text="搜索"
        bg-color="#f5f6f8"
        @search="onSearch"
        @custom="onSearch" />
    </view>

    <view class="page-container">
      <u-loading-icon v-if="state.loading && !state.items.length" text="加载中..." />

      <view v-else-if="state.items.length" class="card card--flat">
        <view
          v-for="(item, index) in state.items"
          :key="item.id"
          class="user-row flex-row"
          :class="{ 'user-row--border': index < state.items.length - 1 }"
          @click="goDetail(item.id)">
          <u-avatar
            :src="resolveAssetUrl(item.avatarUrl)"
            :text="item.userName.slice(0, 1).toUpperCase()"
            size="44"
            bg-color="#1677ff" />
          <view class="user-row__body flex-1">
            <view class="flex-row">
              <text class="user-row__name text-ellipsis">{{ item.userName }}</text>
              <u-tag
                :text="item.isActive ? '启用' : '禁用'"
                :type="item.isActive ? 'success' : 'info'"
                size="mini"
                custom-style="margin-left: 12rpx; flex-shrink: 0;" />
            </view>
            <text class="user-row__sub muted text-ellipsis">
              {{ item.displayName || item.email || formatDateTime(item.createdAt) }}
            </text>
            <view v-if="item.roles?.length" class="user-row__tags">
              <u-tag
                v-for="role in item.roles.slice(0, 3)"
                :key="role"
                :text="role"
                type="primary"
                plain
                size="mini" />
            </view>
          </view>
          <u-icon name="arrow-right" color="#c0c4cc" size="14" />
        </view>
        <u-loadmore :status="loadStatus" custom-style="padding: 24rpx 0;" />
      </view>

      <empty-view v-else text="暂无用户数据" />
    </view>

    <fab-button
      :visible="canCreate"
      @click="goCreate" />

    <app-tabbar active="users" />
  </view>
</template>

<script setup>
import { computed, reactive, ref } from 'vue';
import { onPullDownRefresh, onReachBottom, onShow } from '@dcloudio/uni-app';
import AppTabbar from '@/components/app-tabbar/app-tabbar.vue';
import EmptyView from '@/components/empty-view/empty-view.vue';
import FabButton from '@/components/fab-button/fab-button.vue';
import env from '@/config/env';
import { createPagedState } from '@/model/page.model';
import * as userService from '@/service/user.service';
import { useUserStore } from '@/store/user';
import { formatDateTime, resolveAssetUrl } from '@/utils/format';

const userStore = useUserStore();
const keyword = ref('');
const state = reactive(createPagedState());

const loadStatus = computed(() => {
  if (state.loading) return 'loading';
  if (state.finished) return 'nomore';
  return 'loadmore';
});

const canCreate = computed(() => userStore.hasPermission('system.users:create'));

onShow(() => {
  if (!userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/login/login' });
    return;
  }
  if (!userStore.hasPermission('system.users:view')) {
    uni.showToast({ title: '无权查看用户', icon: 'none' });
    return;
  }
  loadUsers(true);
});

onPullDownRefresh(async () => {
  await loadUsers(true);
  uni.stopPullDownRefresh();
});

onReachBottom(() => {
  if (!state.finished && !state.loading) {
    state.pageIndex += 1;
    loadUsers(false);
  }
});

function onSearch() {
  loadUsers(true);
}

async function loadUsers(reset) {
  if (!userStore.hasPermission('system.users:view')) return;

  if (reset) {
    state.pageIndex = 1;
    state.finished = false;
    state.items = [];
  }

  state.loading = true;
  try {
    const data = await userService.loadUsers({
      pageIndex: state.pageIndex,
      pageSize: env.defaultPageSize,
      keyword: keyword.value.trim()
    });
    const items = data?.items || [];
    state.total = data?.total || 0;
    state.items = reset ? items : [...state.items, ...items];
    state.finished = state.items.length >= state.total;
  } catch (err) {
    uni.showToast({ title: err.message || '加载失败', icon: 'none' });
  } finally {
    state.loading = false;
  }
}

function goDetail(id) {
  uni.navigateTo({ url: `/pages/system/user-detail/user-detail?id=${id}` });
}

function goCreate() {
  uni.navigateTo({ url: '/pages/system/user-form/user-form' });
}
</script>

<style scoped lang="scss">
.search-wrap {
  padding: 20rpx $page-padding;
  background: #fff;
  border-bottom: 1rpx solid $uni-border-color;
}

.user-row {
  padding: 28rpx 0;

  &--border {
    border-bottom: 1rpx solid $uni-border-color;
  }

  &__body {
    margin: 0 20rpx;
  }

  &__name {
    font-size: 30rpx;
    font-weight: 600;
    color: $uni-text-color;
    max-width: 280rpx;
  }

  &__sub {
    display: block;
    margin-top: 8rpx;
    font-size: 24rpx;
  }

  &__tags {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    gap: 8rpx;
    margin-top: 12rpx;
  }
}
</style>
